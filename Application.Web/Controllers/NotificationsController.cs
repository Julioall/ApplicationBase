using Application.Domain;
using Application.Domain.Model.Notification;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Application.Domain.Model;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public NotificationsController(INotificationService notificationService, IStringLocalizer<SharedResource> localizer)
        {
            _notificationService = notificationService;
            _localizer = localizer;
        }

        [HttpGet]
        [ProducesResponseType(typeof(NotificationListResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLatest([FromQuery] int take = 15, CancellationToken cancellationToken = default)
        {
            var result = await _notificationService.GetLatestAsync(take, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MarkAsRead(string id, CancellationToken cancellationToken)
        {
            await _notificationService.MarkAsReadAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("read-all")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkManyAsRead([FromBody] MarkNotificationsRequest? request, CancellationToken cancellationToken)
        {
            if (request == null || request.Ids == null || request.Ids.Count == 0)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["NotificationsIdsRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            await _notificationService.MarkManyAsReadAsync(request.Ids, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await _notificationService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

    }

    public class MarkNotificationsRequest
    {
        public IReadOnlyCollection<string> Ids { get; set; } = Array.Empty<string>();
    }
}
