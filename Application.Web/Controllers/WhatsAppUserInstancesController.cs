using Application.Domain;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.WhatsApp;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace Application.Api.Controllers
{
    [Route("api/whatsapp/me/instances")]
    [ApiController]
    [Authorize(Policy = ApplicationPermissions.ManageWhatsAppSelf)]
    public class WhatsAppUserInstancesController : ControllerBase
    {
        private readonly IWhatsAppInstanceService _instanceService;
        private readonly IUserService _userService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public WhatsAppUserInstancesController(
            IWhatsAppInstanceService instanceService,
            IUserService userService,
            IStringLocalizer<SharedResource> localizer)
        {
            _instanceService = instanceService ?? throw new ArgumentNullException(nameof(instanceService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WhatsAppInstanceResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<WhatsAppInstanceResponse>>> GetMine(CancellationToken cancellationToken = default)
        {
            var ownerId = await GetOwnerUserIdAsync();
            var instances = await _instanceService.GetUserInstancesAsync(ownerId, cancellationToken);
            return Ok(instances.Select(MapUserResponse));
        }

        [HttpPost]
        [ProducesResponseType(typeof(WhatsAppInstanceResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WhatsAppInstanceResponse>> Create([FromBody] CreateWhatsAppInstanceRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRequestDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var ownerId = await GetOwnerUserIdAsync();
            var instance = await _instanceService.CreateUserInstanceAsync(ownerId, request, cancellationToken);
            return CreatedAtAction(nameof(GetMine), new { id = instance.Id }, MapUserResponse(instance));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(WhatsAppInstanceResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<WhatsAppInstanceResponse>> Rename(string id, [FromBody] UpdateWhatsAppInstanceRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRequestDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var ownerId = await GetOwnerUserIdAsync();
            var instance = await _instanceService.RenameInstanceAsync(ownerId, id, request, false, cancellationToken);
            return Ok(MapUserResponse(instance));
        }

        [HttpPost("{id}/qr")]
        [ProducesResponseType(typeof(WhatsAppInstanceQrResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<WhatsAppInstanceQrResponse>> GetQr(string id, CancellationToken cancellationToken = default)
        {
            var ownerId = await GetOwnerUserIdAsync();
            var qr = await _instanceService.GetQrCodeAsync(ownerId, id, false, cancellationToken);
            return Ok(new WhatsAppInstanceQrResponse { QrCode = qr });
        }

        [HttpGet("{id}/status")]
        [ProducesResponseType(typeof(WhatsAppInstanceStatusResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<WhatsAppInstanceStatusResponse>> GetStatus(string id, CancellationToken cancellationToken = default)
        {
            var ownerId = await GetOwnerUserIdAsync();
            var instance = await _instanceService.RefreshStatusAsync(ownerId, id, false, cancellationToken);
            return Ok(new WhatsAppInstanceStatusResponse { Status = instance.Status, IsActive = instance.IsActive });
        }

        [HttpPost("{id}/disconnect")]
        [ProducesResponseType(typeof(WhatsAppInstanceResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<WhatsAppInstanceResponse>> Disconnect(string id, CancellationToken cancellationToken = default)
        {
            var ownerId = await GetOwnerUserIdAsync();
            var instance = await _instanceService.DisconnectInstanceAsync(ownerId, id, false, cancellationToken);
            return Ok(MapUserResponse(instance));
        }

        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(WhatsAppInstanceResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<WhatsAppInstanceResponse>> Delete(string id, CancellationToken cancellationToken = default)
        {
            var ownerId = await GetOwnerUserIdAsync();
            var instance = await _instanceService.DeleteInstanceAsync(ownerId, id, false, cancellationToken);
            return Ok(MapUserResponse(instance));
        }

        private async Task<string> GetOwnerUserIdAsync()
        {
            var email = GetAuthenticatedEmail();
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new UnauthorizedAccessException();
            }

            var user = await _userService.GetByEmailAsync(email);
            if (user == null || string.IsNullOrWhiteSpace(user.Id))
            {
                throw new UnauthorizedAccessException();
            }

            return user.Id!;
        }

        private string? GetAuthenticatedEmail()
        {
            var claimOrder = new[]
            {
                ClaimTypes.Email,
                "email",
                ClaimTypes.Name,
                ClaimTypes.NameIdentifier
            };

            foreach (var claimType in claimOrder)
            {
                var value = User.FindFirstValue(claimType);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return User.Identity?.Name;
        }

        private static WhatsAppInstanceResponse MapUserResponse(WhatsAppInstance instance)
        {
            return new WhatsAppInstanceResponse
            {
                Id = instance.Id,
                DisplayName = instance.DisplayName,
                Status = instance.Status,
                IsActive = instance.IsActive,
                CreatedAt = instance.CreatedAt,
                OwnerUserId = instance.OwnerUserId,
                PhoneNumber = instance.PhoneNumber
            };
        }
    }
}
