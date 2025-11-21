using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Application.Service.Service.Security;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Service.Service
{
    public class TokenService : ITokenService
    {
        private readonly IUserService _userService;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IUserService userService, ILogger<TokenService> logger)
        {
            _userService = userService;
            _logger = logger;
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

        private static JwtSecurityToken CreateJwt(User user)
        {
            var signingKeyValue = Environment.GetEnvironmentVariable(ApplicationConstants.JWT_SIGNING_KEY);
            if (string.IsNullOrWhiteSpace(signingKeyValue))
            {
                throw new ArgumentNullException(nameof(ApplicationConstants.JWT_SIGNING_KEY), "JWT signing key is not configured.");
            }

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyValue));
            var signinCredentials = new SigningCredentials(secretKey, algorithm: SecurityAlgorithms.HmacSha256);
            return new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable(ApplicationConstants.JWT_ISSUER_KEY),
                audience: Environment.GetEnvironmentVariable(ApplicationConstants.JWT_AUDIENCE_KEY),
                claims: new[]
                {
                    new Claim(type: ClaimTypes.Name, user.Account.Email),
                    new Claim(type: ClaimTypes.Email, user.Account.Email),
                    new Claim(type: JwtRegisteredClaimNames.Email, user.Account.Email),
                    new Claim(type: ClaimTypes.Role, user.Account.Role)
                },
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
