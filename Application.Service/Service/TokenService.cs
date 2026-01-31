using Application.Domain.Exceptions;
using Application.Domain.Localization;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Application.Service.Service.Moodle;
using Application.Service.Service.Security;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Service.Service
{
    public class TokenService : ITokenService
    {
        private readonly Application.Shared.Session.ISessionTokenCache _sessionTokenCache;
        private readonly IUserService _userService;
        private readonly ILogger<TokenService> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IMoodleAuthClient _moodleAuthClient;

        public TokenService(
            IUserService userService,
            ILogger<TokenService> logger,
            IStringLocalizer<SharedResource> localizer,
            IMoodleAuthClient moodleAuthClient,
            Application.Shared.Session.ISessionTokenCache sessionTokenCache)
        {
            _userService = userService;
            _logger = logger;
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _moodleAuthClient = moodleAuthClient ?? throw new ArgumentNullException(nameof(moodleAuthClient));
            _sessionTokenCache = sessionTokenCache ?? throw new ArgumentNullException(nameof(sessionTokenCache));
        }

        public async Task<TokenResponseDto?> GenerateTokens(LoginDto loginDto)
        {
            if (loginDto == null)
            {
                return null;
            }

            var userDataBase = await _userService.GetByEmailAsync(loginDto.Email);

            if (userDataBase is null ||
                userDataBase.Account.PasswordHash == null ||
                !SecureHash.Verify(loginDto.Password, userDataBase.Account.PasswordHash))
            {
                return null;
            }

            var jwtSecurityToken = CreateJwt(userDataBase);
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

            var refreshToken = GenerateRefreshToken();
            userDataBase.Account.RefreshTokenId = refreshToken.Id;
            userDataBase.Account.RefreshTokenHash = refreshToken.Hash;
            userDataBase.Account.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            userDataBase.Account.LastLogin = DateTime.UtcNow;
            await _userService.UpdateAsync(userDataBase);

            return new TokenResponseDto
            {
                Token = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = jwtSecurityToken.ValidTo
            };
        }

        public async Task<TokenResponseDto?> RefreshAsync(string refreshToken)
        {
            if (!TryParseRefreshToken(refreshToken, out var tokenId, out var tokenSecret))
            {
                _logger.LogWarning("Refresh token rejected: invalid format");
                return null;
            }

            var user = await _userService.GetByRefreshTokenAsync(tokenId);
            if (user == null || user.Account.RefreshTokenExpiry is null || user.Account.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token rejected: user missing or token expired");
                return null;
            }

            if (!SecureHash.Verify(tokenSecret, user.Account.RefreshTokenHash))
            {
                _logger.LogWarning("Refresh token rejected: hash mismatch");
                return null;
            }

            var jwtSecurityToken = CreateJwt(user);
            var newAccessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

            var newRefreshToken = GenerateRefreshToken();
            user.Account.RefreshTokenId = newRefreshToken.Id;
            user.Account.RefreshTokenHash = newRefreshToken.Hash;
            user.Account.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userService.UpdateAsync(user);

            return new TokenResponseDto
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = jwtSecurityToken.ValidTo
            };
        }

        public async Task<TokenResponseDto?> GenerateMoodleTokens(MoodleLoginDto loginDto)
        {
            if (loginDto == null)
            {
                return null;
            }

            var token = await _moodleAuthClient.AuthenticateAsync(loginDto.Username, loginDto.Password);
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Moodle authentication failed for user {Username}", loginDto.Username);
                return null;
            }

            var siteInfo = await _moodleAuthClient.GetSiteInfoAsync(token);
            if (siteInfo == null)
            {
                _logger.LogWarning("Moodle site info unavailable for token issued to {Username}", loginDto.Username);
                return null;
            }
            // Salvar token no cache de sessão
            if (int.TryParse(siteInfo.UserId?.ToString(), out var moodleUserId))
            {
                _sessionTokenCache.SetMoodleToken(moodleUserId, token, TimeSpan.FromHours(2));
            }

            var email = !string.IsNullOrWhiteSpace(siteInfo.Email) ? siteInfo.Email : siteInfo.UserName;
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Moodle user info missing email/username for id {UserId}", siteInfo.UserId);
                return null;
            }

            var name = string.IsNullOrWhiteSpace(siteInfo.FullName) ? siteInfo.UserName : siteInfo.FullName;
            var permissions = ApplicationPermissions.DefaultUserPermissions;

            // Persistir usuário Moodle no RavenDB sem exigir senha
            var user = new User
            {
                Account = new UserAccount
                {
                    Email = email,
                    Username = siteInfo.UserName,
                    MoodleId = siteInfo.UserId?.ToString(),
                    Permissions = permissions.ToList(),
                    DateJoined = DateTime.UtcNow
                },
                Profile = new UserProfile
                {
                    Name = name
                }
            };
            if (_userService is Application.Service.Service.UserService concreteUserService)
            {
                await concreteUserService.AddOrUpdateExternalUserAsync(user);
            }
            else
            {
                await _userService.UpdateAsync(user);
            }

            var baseClaims = new List<Claim>
            {
                new(type: ClaimTypes.Email, email),
                new(type: JwtRegisteredClaimNames.Email, email),
                new(type: ClaimTypes.Name, name),
                new(type: "name", name),
                new(type: "username", siteInfo.UserName ?? email),
                new(type: JwtRegisteredClaimNames.Sub, siteInfo.UserId?.ToString() ?? string.Empty),
                new(type: "auth_provider", "moodle")
            };

            if (!string.IsNullOrWhiteSpace(siteInfo.PictureUrl))
            {
                baseClaims.Add(new Claim("picture", siteInfo.PictureUrl));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.FirstName))
            {
                baseClaims.Add(new Claim(JwtRegisteredClaimNames.GivenName, siteInfo.FirstName));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.LastName))
            {
                baseClaims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, siteInfo.LastName));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.City))
            {
                baseClaims.Add(new Claim("city", siteInfo.City));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.Country))
            {
                baseClaims.Add(new Claim("country", siteInfo.Country));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.Department))
            {
                baseClaims.Add(new Claim("department", siteInfo.Department));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.Institution))
            {
                baseClaims.Add(new Claim("organization", siteInfo.Institution));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.Phone1))
            {
                baseClaims.Add(new Claim("phone1", siteInfo.Phone1));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.Phone2))
            {
                baseClaims.Add(new Claim("phone2", siteInfo.Phone2));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.Description))
            {
                baseClaims.Add(new Claim("bio", siteInfo.Description));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.Lang))
            {
                baseClaims.Add(new Claim("locale", siteInfo.Lang));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.TimeZone))
            {
                baseClaims.Add(new Claim("zoneinfo", siteInfo.TimeZone));
            }

            if (!string.IsNullOrWhiteSpace(siteInfo.IdNumber))
            {
                baseClaims.Add(new Claim("idnumber", siteInfo.IdNumber));
            }

            var jwtSecurityToken = CreateJwt(baseClaims, permissions);
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

            return new TokenResponseDto
            {
                Token = accessToken,
                RefreshToken = (string?)null,
                ExpiresAt = jwtSecurityToken.ValidTo
            };
        }

        private JwtSecurityToken CreateJwt(User user)
        {
            user.Account.Permissions ??= new List<string>();

            var claims = new List<Claim>
            {
                new(type: ClaimTypes.Name, user.Account.Email),
                new(type: ClaimTypes.Email, user.Account.Email),
                new(type: JwtRegisteredClaimNames.Email, user.Account.Email)
            };

            return CreateJwt(claims, user.Account.Permissions);
        }

        private JwtSecurityToken CreateJwt(IEnumerable<Claim> baseClaims, IEnumerable<string>? permissions)
        {
            var signingKeyValue = Environment.GetEnvironmentVariable(ApplicationConstants.JWT_SIGNING_KEY);
            if (string.IsNullOrWhiteSpace(signingKeyValue))
            {
                throw new ConfigurationException(_localizer["JwtSigningKeyNotConfigured"]);
            }

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyValue));
            var signinCredentials = new SigningCredentials(secretKey, algorithm: SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>(baseClaims ?? Enumerable.Empty<Claim>());

            var normalizedPermissions = permissions?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>();

            foreach (var permission in normalizedPermissions)
            {
                claims.Add(new Claim(ApplicationPermissions.PermissionClaimType, permission));
            }

            return new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable(ApplicationConstants.JWT_ISSUER_KEY),
                audience: Environment.GetEnvironmentVariable(ApplicationConstants.JWT_AUDIENCE_KEY),
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: signinCredentials
                );
        }

        /// <summary>
        /// Gera refresh token composto (id.cs) + hash, usado para rotação e revogação.
        /// </summary>
        private static RefreshTokenPayload GenerateRefreshToken()
        {
            var tokenId = Guid.NewGuid().ToString("N");
            var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var hash = SecureHash.HashSecret(secret);
            var token = $"{tokenId}.{secret}";
            return new RefreshTokenPayload(tokenId, secret, hash, token);
        }

        private static bool TryParseRefreshToken(string refreshToken, out string tokenId, out string tokenSecret)
        {
            tokenId = string.Empty;
            tokenSecret = string.Empty;
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return false;
            }

            var parts = refreshToken.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            tokenId = parts[0];
            tokenSecret = parts[1];
            return !string.IsNullOrWhiteSpace(tokenId) && !string.IsNullOrWhiteSpace(tokenSecret);
        }

        private readonly record struct RefreshTokenPayload(string Id, string Secret, string Hash, string Token);
    }
}
