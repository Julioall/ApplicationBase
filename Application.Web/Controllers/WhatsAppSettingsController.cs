using Application.Domain.Localization;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Service.Interface;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Application.Api.Controllers
{
    [Route("api/settings/whatsapp")]
    [ApiController]
    [Authorize]
    public class WhatsAppSettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IValidator<WhatsAppSettingsRequest> _validator;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public WhatsAppSettingsController(
            ISettingsService settingsService,
            IValidator<WhatsAppSettingsRequest> validator,
            IStringLocalizer<SharedResource> localizer)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        [HttpGet]
        [ProducesResponseType(typeof(WhatsAppSettings), StatusCodes.Status200OK)]
        public async Task<ActionResult<WhatsAppSettings>> Get()
        {
            var settings = await _settingsService.GetWhatsAppAsync();
            return Ok(settings);
        }

        [HttpPut]
        [Authorize(Policy = ApplicationPermissions.ManageWhatsApp)]
        [ProducesResponseType(typeof(WhatsAppSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WhatsAppSettings>> Update([FromBody] WhatsAppSettingsRequest request)
        {
            if (request == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRequestDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            await _validator.ValidateAndThrowAsync(request);

            var settings = new WhatsAppSettings
            {
                MaxUserInstances = request.MaxUserInstances
            };

            var saved = await _settingsService.SaveWhatsAppAsync(settings);
            return Ok(saved);
        }
    }
}
