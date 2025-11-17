using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Service.Service
{
    public class TokenService : ITokenService
    {
        private readonly IUserService _userService;

        public TokenService(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<TokenResponseDto?> GenerateTokens(LoginDto loginDto)
        {
            var userDataBase = await _userService.GetByEmailAsync(loginDto.Email);

            if (userDataBase is null || loginDto is null || userDataBase.Account.Email != loginDto.Email || userDataBase.Account.Password != loginDto.Password)
            {
                return null;
            }

            var jwtSecurityToken = CreateJwt(userDataBase);
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

            var refreshToken = GenerateRefreshToken();
            userDataBase.Account.RefreshToken = refreshToken;
            userDataBase.Account.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            userDataBase.Account.LastLogin = DateTime.UtcNow;
            await _userService.UpdateAsync(userDataBase);

            return new TokenResponseDto
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = jwtSecurityToken.ValidTo
            };
        }

        public async Task<TokenResponseDto?> RefreshAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            var user = await _userService.GetByRefreshTokenAsync(refreshToken);
            if (user == null || user.Account.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                return null;
            }

            var jwtSecurityToken = CreateJwt(user);
            var newAccessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

            var newRefreshToken = GenerateRefreshToken();
            user.Account.RefreshToken = newRefreshToken;
            user.Account.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userService.UpdateAsync(user);

            return new TokenResponseDto
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = jwtSecurityToken.ValidTo
            };
        }

        private static JwtSecurityToken CreateJwt(User user)
        {
            if (string.IsNullOrWhiteSpace(ApplicationConstants.JWT_SIGNING_KEY))
            {
                throw new ArgumentNullException(nameof(ApplicationConstants.JWT_SIGNING_KEY), "JWT signing key is not configured.");
            }

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApplicationConstants.JWT_SIGNING_KEY));
            var signinCredentials = new SigningCredentials(secretKey, algorithm: SecurityAlgorithms.HmacSha256);
            return new JwtSecurityToken(
                issuer: ApplicationConstants.JWT_ISSUER,
                audience: ApplicationConstants.JWT_AUDIENCE,
                claims: new[]
                {
                    new Claim(type: ClaimTypes.Name, user.Account.Email),
                    new Claim(type: ClaimTypes.Role, user.Account.Role)
                },
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: signinCredentials
                );
        }

        private static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
    }
}
