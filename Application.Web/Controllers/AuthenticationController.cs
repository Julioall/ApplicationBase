using Application.Domain.Model.Dtos;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly ITokenService _tokenService;

        public AuthenticationController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto == null)
            {
                return BadRequest("Login information is missing.");
            }

            var tokenResponse = await _tokenService.GenerateTokens(loginDto);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
            {
                return Unauthorized("Invalid login credentials.");
            }

            return Ok(new { token = tokenResponse.Token, refreshToken = tokenResponse.RefreshToken, expiresAt = tokenResponse.ExpiresAt });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult> Refresh([FromBody] RefreshRequestDto request)
        {
            var tokenResponse = await _tokenService.RefreshAsync(request.RefreshToken);
            if (tokenResponse == null)
            {
                return Unauthorized("Invalid refresh token.");
            }

            return Ok(new { token = tokenResponse.Token, refreshToken = tokenResponse.RefreshToken, expiresAt = tokenResponse.ExpiresAt });
        }
    }
}
