using Application.Domain;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ISettingsService _settingsService;
        private readonly EmailSettings _settings;

        public EmailController(
            IEmailService emailService,
            IUserService userService,
            IStringLocalizer<SharedResource> localizer,
            IOptions<EmailSettings> settings,
            ISettingsService settingsService)
        {
            _emailService = emailService;
            _userService = userService;
            _localizer = localizer;
            _settings = settings.Value;
            _settingsService = settingsService;
        }

        [AllowAnonymous]
        [HttpPost("reset")]
        public async Task<ActionResult> SendReset([FromBody] SendResetEmailDto request)
        {
            var targetEmail = request?.Email;
            if (string.IsNullOrWhiteSpace(targetEmail))
            {
                targetEmail = GetAuthenticatedEmail();
            }

            if (string.IsNullOrWhiteSpace(targetEmail))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["EmailRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                var user = await _userService.GetByEmailAsync(targetEmail);
                if (user != null)
                {
                    await _emailService.SendPasswordResetAsync(targetEmail);
                }
            }
            catch (InvalidOperationException ex)
            {
                return Problem(title: _localizer["InternalErrorTitle"], detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception)
            {
                return Problem(title: _localizer["InternalErrorTitle"], detail: _localizer["InternalErrorDetail"], statusCode: StatusCodes.Status500InternalServerError);
            }

            return Ok(new { message = _localizer["ResetEmailSent"] });
        }

        private string? GetAuthenticatedEmail()
        {
            return User?.FindFirstValue(ClaimTypes.Email)
                ?? User?.FindFirstValue("email")
                ?? User?.FindFirstValue(ClaimTypes.Name);
        }

        [Authorize]
        [HttpGet("settings")]
        public async Task<ActionResult<EmailSettings>> GetSettings()
        {
            var email = await _settingsService.GetEmailAsync();
            return Ok(email);
        }

        [Authorize]
        [HttpPut("settings")]
        public async Task<ActionResult> UpdateSettings([FromBody] EmailSettings settings)
        {
            if (settings == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidOperationTitle"], statusCode: StatusCodes.Status400BadRequest);
            }

            var currentEmail = await _settingsService.GetEmailAsync();

            if (string.IsNullOrWhiteSpace(settings.Password))
            {
                settings.Password = currentEmail.Password;
            }

            if (string.IsNullOrWhiteSpace(settings.Host) ||
                string.IsNullOrWhiteSpace(settings.FromEmail) ||
                string.IsNullOrWhiteSpace(settings.Password) ||
                settings.Port <= 0)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: "Host, remetente, senha e porta são obrigatórios.", statusCode: StatusCodes.Status400BadRequest);
            }

            var saved = await _settingsService.SaveEmailAsync(settings);
            return Ok(saved);
        }

        [Authorize]
        [HttpPost("test")]
        public async Task<ActionResult> SendTest([FromBody] SendResetEmailDto? request)
        {
            var targetEmail = request?.Email ?? GetAuthenticatedEmail();
            if (string.IsNullOrWhiteSpace(targetEmail))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["EmailRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                await _emailService.SendTestEmailAsync(targetEmail, request?.Settings);
            }
            catch (InvalidOperationException ex)
            {
                return Problem(title: _localizer["InternalErrorTitle"], detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception)
            {
                return Problem(title: _localizer["InternalErrorTitle"], detail: _localizer["InternalErrorDetail"], statusCode: StatusCodes.Status500InternalServerError);
            }

            return Ok(new { message = _localizer["ResetEmailSent"], email = targetEmail });
        }
    }
}
