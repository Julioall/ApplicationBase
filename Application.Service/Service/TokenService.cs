using Application.Domain.Model;
using Application.Service.Dtos;
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

        public async Task<string> GenerateToken(LoginDto loginDto)
        {
            var userDataBase = await _userService.GetByEmailAsync(loginDto.Email);

            if (userDataBase is null || loginDto is null || userDataBase?.Account?.Email != loginDto?.Email || userDataBase?.Account?.Password != loginDto?.Password)
            {
                return string.Empty;
            }

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AplicationConstants.JWT_SIGNING_KEY ?? string.Empty));
            var signinCredentials = new SigningCredentials(secretKey, algorithm: SecurityAlgorithms.HmacSha256);
            var tokenOptions = new JwtSecurityToken(
                issuer: AplicationConstants.JWT_ISSUER,
                audience: AplicationConstants.JWT_AUDIENCE,
                claims: new[]
                {
                    new Claim(type: ClaimTypes.Name, userDataBase.Account.Email),
                    new Claim(type: ClaimTypes.Role, userDataBase.Account.Role)
                },
                expires: DateTime.Now.AddHours(2),
                signingCredentials: signinCredentials
                );
            var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            return token;
        }
    }
}
