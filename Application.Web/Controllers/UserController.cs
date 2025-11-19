using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UserController(IUserService userService, IStringLocalizer<SharedResource> localizer)
        {
            _userService = userService;
            _localizer = localizer;
        }

        [AllowAnonymous]
        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddUser([FromBody] User user)
        {
            if (user == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            await _userService.AddAsync(user);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new { message = _localizer["UserAddedSuccessfully"] });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundById", id]);
            }

            await _userService.DeleteAsync(id);
            return NoContent();
        }

        [Authorize(Roles = "Admin, User")]
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [Authorize(Roles = "Admin, User")]
        [HttpGet("get/{id}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> GetUserById(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundById", id]);
            }

            await PopulateProfilePictureAsync(user);
            return Ok(user);
        }

        [Authorize(Roles = "Admin, User")]
        [HttpGet("role/{role}")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersByRole(string role)
        {
            var users = await _userService.GetByRoleAsync(role);
            return Ok(users);
        }

        [Authorize(Roles = "Admin, User")]
        [HttpGet("username/{username}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> GetUserByEmail(string username)
        {
            var user = await _userService.GetByEmailAsync(username);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundByEmail", username]);
            }

            await PopulateProfilePictureAsync(user);
            return Ok(user);
        }

        [Authorize(Roles = "Admin, User")]
        [HttpGet("me")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<User>> GetCurrentUser()
        {
            var email = GetAuthenticatedEmail();
            if (string.IsNullOrWhiteSpace(email))
            {
                return Problem(title: _localizer["UnauthorizedTitle"], detail: _localizer["UnauthorizedDetail"], statusCode: StatusCodes.Status401Unauthorized);
            }

            var user = await _userService.GetByEmailAsync(email);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundByEmail", email]);
            }

            await PopulateProfilePictureAsync(user);
            return Ok(user);
        }

        [Authorize(Roles = "Admin, User")]
        [HttpPut("update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser([FromBody] User user)
        {
            if (user.Id.IsNullOrEmpty())
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var existingUser = await _userService.GetByIdAsync(user.Id);
            if (existingUser == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundById", user.Id]);
            }

            await _userService.UpdateAsync(user);
            return Ok(new { message = _localizer["UserUpdatedSuccessfully"] });
        }

        [Authorize(Roles = "Admin, User")]
        [HttpPut("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto profileDto)
        {
            if (profileDto == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var email = GetAuthenticatedEmail();
            if (string.IsNullOrWhiteSpace(email))
            {
                return Problem(title: _localizer["UnauthorizedTitle"], detail: _localizer["UnauthorizedDetail"], statusCode: StatusCodes.Status401Unauthorized);
            }

            await _userService.UpdateProfileAsync(email, profileDto.Name, profileDto.DateOfBirth, profileDto.ProfilePictureUrl);
            return Ok(new { message = _localizer["UserProfileUpdatedSuccessfully"] });
        }

        [Authorize(Roles = "Admin, User")]
        [HttpPut("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto passwordDto)
        {
            if (passwordDto == null || passwordDto.CurrentPassword.IsNullOrEmpty() || passwordDto.NewPassword.IsNullOrEmpty())
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var email = GetAuthenticatedEmail();
            if (string.IsNullOrWhiteSpace(email))
            {
                return Problem(title: _localizer["UnauthorizedTitle"], detail: _localizer["UnauthorizedDetail"], statusCode: StatusCodes.Status401Unauthorized);
            }

            await _userService.ChangePasswordAsync(email, passwordDto.CurrentPassword, passwordDto.NewPassword);
            return Ok(new { message = _localizer["UserPasswordUpdatedSuccessfully"] });
        }

        private async Task PopulateProfilePictureAsync(User user)
        {
            if (user?.Id.IsNullOrEmpty() ?? true)
            {
                return;
            }

            var attachment = await _userService.GetProfilePictureAsync(user.Id);
            if (attachment?.Data != null && attachment.Value.Data.Length > 0)
            {
                var contentType = string.IsNullOrWhiteSpace(attachment.Value.ContentType) ? "image/png" : attachment.Value.ContentType;
                var base64 = Convert.ToBase64String(attachment.Value.Data);
                user.Profile ??= new UserProfile { Name = user.Profile?.Name ?? string.Empty };
                user.Profile.ProfilePictureUrl = $"data:{contentType};base64,{base64}";
            }
            else if (user?.Profile != null)
            {
                user.Profile.ProfilePictureUrl = null;
            }
        }

        private string? GetAuthenticatedEmail()
        {
            return User.FindFirstValue(ClaimTypes.Email)
                   ?? User.FindFirstValue(ClaimTypes.Name)
                   ?? User.Identity?.Name;
        }
    }
}
