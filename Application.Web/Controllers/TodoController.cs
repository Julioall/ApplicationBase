using Application.Domain;
using Application.Domain.Model;
using Application.Domain.Model.Todo;
using Application.Domain.Model.Todo.Dtos;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using System.IO;
using System.Security.Claims;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoController : ControllerBase
    {
        private const long MaxImageBytes = 5 * 1024 * 1024;

        private readonly ITodoService _todoService;
        private readonly IUserService _userService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public TodoController(ITodoService todoService, IUserService userService, IStringLocalizer<SharedResource> localizer)
        {
            _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        [HttpGet]
        [Authorize(Policy = ApplicationPermissions.ViewTodo)]
        [ProducesResponseType(typeof(IEnumerable<TodoTaskDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTasks([FromQuery] TodoTaskSearchQuery? query)
        {
            var criteria = query ?? new TodoTaskSearchQuery();
            var tasks = await _todoService.GetTasksAsync(criteria);
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = ApplicationPermissions.ViewTodo)]
        [ProducesResponseType(typeof(TodoTaskDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(string id)
        {
            var task = await _todoService.GetByIdAsync(id);
            return Ok(task);
        }

        [HttpPost("images")]
        [Authorize(Policy = ApplicationPermissions.ManageTodo)]
        [RequestSizeLimit(MaxImageBytes)]
        [ProducesResponseType(typeof(TodoImageUploadResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRequestDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["ProfilePictureContentTypeInvalid"], statusCode: StatusCodes.Status400BadRequest);
            }

            if (file.Length > MaxImageBytes)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["ProfilePictureTooLarge"], statusCode: StatusCodes.Status400BadRequest);
            }

            var userId = await RequireCurrentUserIdAsync();
            await using var stream = file.OpenReadStream();
            var imageId = await _todoService.UploadImageAsync(userId, stream, file.ContentType, file.FileName ?? "image");

            var url = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/todo/images/{Uri.EscapeDataString(imageId)}";
            return Ok(new TodoImageUploadResultDto { Url = url, ImageId = imageId });
        }

        [HttpGet("images/{imageId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetImage(string imageId)
        {
            var image = await _todoService.GetImageAsync(imageId);
            if (image == null)
            {
                return NotFound();
            }

            return File(image.Value.Data, image.Value.ContentType);
        }

        [HttpPost]
        [Authorize(Policy = ApplicationPermissions.ManageTodo)]
        [ProducesResponseType(typeof(TodoTaskDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateTodoTaskDto? dto)
        {
            if (dto == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRequestDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var userId = await RequireCurrentUserIdAsync();
            var created = await _todoService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = ApplicationPermissions.ManageTodo)]
        [ProducesResponseType(typeof(TodoTaskDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTodoTaskDto? dto)
        {
            if (dto == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRequestDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var updated = await _todoService.UpdateAsync(id, dto);
            return Ok(updated);
        }

        [HttpPost("{id}/archive")]
        [Authorize(Policy = ApplicationPermissions.ManageTodo)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Archive(string id)
        {
            await _todoService.ArchiveAsync(id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = ApplicationPermissions.ManageTodo)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(string id)
        {
            await _todoService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/steps")]
        [Authorize(Policy = ApplicationPermissions.ManageTodo)]
        [ProducesResponseType(typeof(TodoTaskDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddStep(string id, [FromBody] CreateTodoStepDto? dto)
        {
            if (dto == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRequestDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var task = await _todoService.AddStepAsync(id, dto);
            return Ok(task);
        }

        [HttpPut("{id}/steps/{stepId}")]
        [Authorize(Policy = ApplicationPermissions.ManageTodo)]
        [ProducesResponseType(typeof(TodoTaskDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStep(string id, string stepId, [FromBody] UpdateTodoStepDto? dto)
        {
            if (dto == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRequestDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var task = await _todoService.UpdateStepAsync(id, stepId, dto);
            return Ok(task);
        }

        [HttpPost("{id}/steps/reorder")]
        [Authorize(Policy = ApplicationPermissions.ManageTodo)]
        [ProducesResponseType(typeof(TodoTaskDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReorderSteps(string id, [FromBody] ReorderTodoStepsDto? dto)
        {
            if (dto == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRequestDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var task = await _todoService.ReorderStepsAsync(id, dto);
            return Ok(task);
        }

        [HttpDelete("{id}/steps/{stepId}")]
        [Authorize(Policy = ApplicationPermissions.ManageTodo)]
        [ProducesResponseType(typeof(TodoTaskDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteStep(string id, string stepId)
        {
            var task = await _todoService.DeleteStepAsync(id, stepId);
            return Ok(task);
        }

        private async Task<string> RequireCurrentUserIdAsync()
        {
            var userId = await TryGetCurrentUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException();
            }

            return userId;
        }

        private async Task<string?> TryGetCurrentUserIdAsync()
        {
            var email = GetAuthenticatedEmail();
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var user = await _userService.GetByEmailAsync(email);
            return user?.Id;
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
    }
}
