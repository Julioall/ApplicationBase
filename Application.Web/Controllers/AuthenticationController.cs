using Application.Domain;
using Application.Domain.Model.Dtos;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AuthenticationController(ITokenService tokenService, IStringLocalizer<SharedResource> localizer)
        {
            _tokenService = tokenService;
            _localizer = localizer;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["LoginInformationMissing"], statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(loginDto.Email))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["EmailRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["PasswordRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            var tokenResponse = await _tokenService.GenerateTokens(loginDto);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
            {
                return Problem(title: _localizer["UnauthorizedTitle"], detail: _localizer["InvalidLoginCredentials"], statusCode: StatusCodes.Status401Unauthorized);
            }

            return Ok(new { token = tokenResponse.Token, refreshToken = tokenResponse.RefreshToken, expiresAt = tokenResponse.ExpiresAt });
        }

        [AllowAnonymous]
        [HttpPost("moodle/login")]
        public async Task<ActionResult> MoodleLogin([FromBody] MoodleLoginDto moodleLoginDto)
        {
            if (moodleLoginDto == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["LoginInformationMissing"], statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(moodleLoginDto.Username))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["EmailRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(moodleLoginDto.Password))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["PasswordRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            var tokenResponse = await _tokenService.GenerateMoodleTokens(moodleLoginDto);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
            {
                return Problem(title: _localizer["UnauthorizedTitle"], detail: _localizer["InvalidLoginCredentials"], statusCode: StatusCodes.Status401Unauthorized);
            }

            return Ok(new { token = tokenResponse.Token, refreshToken = tokenResponse.RefreshToken, expiresAt = tokenResponse.ExpiresAt });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult> Refresh([FromBody] RefreshRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRefreshToken"], statusCode: StatusCodes.Status400BadRequest);
            }

            var tokenResponse = await _tokenService.RefreshAsync(request.RefreshToken);
            if (tokenResponse == null)
            {
                return Problem(title: _localizer["UnauthorizedTitle"], detail: _localizer["InvalidRefreshToken"], statusCode: StatusCodes.Status401Unauthorized);
            }

            return Ok(new { token = tokenResponse.Token, refreshToken = tokenResponse.RefreshToken, expiresAt = tokenResponse.ExpiresAt });
        }
    }
}
