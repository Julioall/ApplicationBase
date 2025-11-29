using Application.Api.Models.User;
using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private const int MaxAvatarBytes = 2 * 1024 * 1024; // 2 MB
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
        public async Task<IActionResult> AddUser([FromBody] CreateUserDto request)
        {
            if (request == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(request.Account?.Password))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["PasswordRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            var user = MapToUser(request);

            await _userService.AddAsync(user, request.Account.Password);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new { message = _localizer["UserAddedSuccessfully"] });
        }

        [Authorize(Policy = ApplicationPermissions.ManageUsers)]
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

        [Authorize(Policy = ApplicationPermissions.ManageUsers)]
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [Authorize(Policy = ApplicationPermissions.ManageUsers)]
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

        [Authorize(Policy = ApplicationPermissions.ManageUsers)]
        [HttpGet("permission/{permission}")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersByPermission(string permission)
        {
            var users = await _userService.GetByPermissionAsync(permission) ?? Enumerable.Empty<User>();
            return Ok(users);
        }

        [Authorize(Policy = ApplicationPermissions.ManageUsers)]
        [HttpGet("permissions")]
        public ActionResult<IEnumerable<string>> GetAvailablePermissions()
        {
            return Ok(ApplicationPermissions.All);
        }

        [Authorize(Policy = ApplicationPermissions.ManageUsers)]
        [HttpGet("email/{email}")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> GetUserByEmail(string email)
        {
            var user = await _userService.GetByEmailAsync(email);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundByEmail", email]);
            }

            await PopulateProfilePictureAsync(user);
            return Ok(user);
        }

        [Authorize(Policy = ApplicationPermissions.ViewProfile)]
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

        [Authorize(Policy = ApplicationPermissions.ManageUsers)]
        [HttpPut("update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser([FromBody] User user)
        {
            if (user == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

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

        [Authorize(Policy = ApplicationPermissions.ManageUsers)]
        [HttpPut("{id}/permissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePermissions(string id, [FromBody] UpdateUserPermissionsRequest? request)
        {
            if (string.IsNullOrWhiteSpace(id) || request == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundById", id]);
            }

            var normalizedPermissions = NormalizePermissions(request.Permissions);
            if (!normalizedPermissions.Any())
            {
                normalizedPermissions = ApplicationPermissions.DefaultUserPermissions.ToList();
            }

            user.Account.Permissions = normalizedPermissions;
            await _userService.UpdateAsync(user);
            return Ok(new { message = _localizer["UserUpdatedSuccessfully"], permissions = normalizedPermissions });
        }

        [Authorize(Policy = ApplicationPermissions.ViewProfile)]
        [HttpPut("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile()
        {
            var email = GetAuthenticatedEmail();
            if (string.IsNullOrWhiteSpace(email))
            {
                return Problem(title: _localizer["UnauthorizedTitle"], detail: _localizer["UnauthorizedDetail"], statusCode: StatusCodes.Status401Unauthorized);
            }

            Stream? profilePictureStream = null;
            string? contentType = null;
            string? name = null;
            DateTime? dateOfBirth = null;
            bool removeProfilePicture = false;
            double? profilePictureOffsetX = null;
            double? profilePictureOffsetY = null;
            double? profilePictureScale = null;
            string? jobTitle = null;
            string? department = null;
            string? organization = null;
            string? location = null;

            try
            {
                if (Request.HasFormContentType)
                {
                    var formRequest = MapFormRequest(await Request.ReadFormAsync());
                    name = formRequest.Name;
                    dateOfBirth = formRequest.DateOfBirth;
                    removeProfilePicture = formRequest.RemoveProfilePicture;
                    profilePictureOffsetX = formRequest.ProfilePictureOffsetX;
                    profilePictureOffsetY = formRequest.ProfilePictureOffsetY;
                    profilePictureScale = formRequest.ProfilePictureScale;
                    jobTitle = formRequest.JobTitle;
                    department = formRequest.Department;
                    organization = formRequest.Organization;
                    location = formRequest.Location;

                    if (formRequest.ProfilePicture is { Length: > 0 })
                    {
                        if (!formRequest.ProfilePicture.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        {
                            return Problem(title: _localizer["InvalidRequestTitle"], detail: "Invalid profile picture content type.", statusCode: StatusCodes.Status400BadRequest);
                        }
                        if (formRequest.ProfilePicture.Length > MaxAvatarBytes)
                        {
                            return Problem(title: _localizer["InvalidRequestTitle"], detail: "Profile picture too large.", statusCode: StatusCodes.Status400BadRequest);
                        }

                        profilePictureStream = formRequest.ProfilePicture.OpenReadStream();
                        contentType = formRequest.ProfilePicture.ContentType;
                    }
                }
                else
                {
                    var profileDto = await Request.ReadFromJsonAsync<UpdateProfileDto>();
                    if (profileDto == null)
                    {
                        return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
                    }

                    name = profileDto.Name;
                    dateOfBirth = profileDto.DateOfBirth;
                    removeProfilePicture = profileDto.RemoveProfilePicture;
                    profilePictureOffsetX = profileDto.ProfilePictureOffsetX;
                    profilePictureOffsetY = profileDto.ProfilePictureOffsetY;
                    profilePictureScale = profileDto.ProfilePictureScale;
                    jobTitle = profileDto.JobTitle;
                    department = profileDto.Department;
                    organization = profileDto.Organization;
                    location = profileDto.Location;

                    if (!removeProfilePicture &&
                        !string.IsNullOrWhiteSpace(profileDto.ProfilePictureUrl) &&
                        TryParseDataUrl(profileDto.ProfilePictureUrl, out var dtoContentType, out var data))
                    {
                        if (data.Length > MaxAvatarBytes)
                        {
                            return Problem(title: _localizer["InvalidRequestTitle"], detail: "Profile picture too large.", statusCode: StatusCodes.Status400BadRequest);
                        }
                        if (!dtoContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        {
                            return Problem(title: _localizer["InvalidRequestTitle"], detail: "Invalid profile picture content type.", statusCode: StatusCodes.Status400BadRequest);
                        }
                        profilePictureStream = new MemoryStream(data);
                        contentType = dtoContentType;
                    }
                }

                await _userService.UpdateProfileAsync(
                    email,
                    name,
                    dateOfBirth,
                    profilePictureStream,
                    contentType,
                    removeProfilePicture,
                    profilePictureOffsetX,
                    profilePictureOffsetY,
                    jobTitle,
                    department,
                    organization,
                    location,
                    profilePictureScale);
                return Ok(new { message = _localizer["UserProfileUpdatedSuccessfully"] });
            }
            finally
            {
                if (profilePictureStream != null)
                {
                    await profilePictureStream.DisposeAsync();
                }
            }
        }

        [Authorize(Policy = ApplicationPermissions.ViewProfile)]
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
            var claimOrder = new[]
            {
                ClaimTypes.Email,
                JwtRegisteredClaimNames.Email,
                "email",
                ClaimTypes.Name,
                ClaimTypes.NameIdentifier,
                ClaimTypes.Upn,
                JwtRegisteredClaimNames.UniqueName
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

        private static bool TryParseDataUrl(string dataUrl, out string contentType, out byte[] data)
        {
            contentType = "application/octet-stream";
            data = Array.Empty<byte>();
            if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var commaIndex = dataUrl.IndexOf(',');
            if (commaIndex < 0)
            {
                return false;
            }

            var metadata = dataUrl.Substring(5, commaIndex - 5);
            var payload = dataUrl[(commaIndex + 1)..];
            var metaParts = metadata.Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (metaParts.Length > 0)
            {
                contentType = metaParts[0];
            }

            try
            {
                data = Convert.FromBase64String(payload);
                return true;
            }
            catch
            {
                data = Array.Empty<byte>();
                return false;
            }
        }

        private static UpdateProfileRequest MapFormRequest(IFormCollection form)
        {
            var request = new UpdateProfileRequest
            {
                Name = form.TryGetValue("Name", out var nameValues) ? nameValues.ToString() : null,
                RemoveProfilePicture = form.TryGetValue("RemoveProfilePicture", out var removeValues) && bool.TryParse(removeValues.ToString(), out var removeFlag) && removeFlag,
                ProfilePicture = form.Files.GetFile("ProfilePicture") ?? form.Files.GetFile("ProfilePictureUrl"),
                ProfilePictureOffsetX = TryParseDouble(form, "ProfilePictureOffsetX"),
                ProfilePictureOffsetY = TryParseDouble(form, "ProfilePictureOffsetY"),
                ProfilePictureScale = TryParseDouble(form, "ProfilePictureScale"),
                JobTitle = GetValue(form, "JobTitle"),
                Department = GetValue(form, "Department"),
                Organization = GetValue(form, "Organization"),
                Location = GetValue(form, "Location")
            };

            if (form.TryGetValue("DateOfBirth", out var dateValues) && DateTime.TryParse(dateValues.ToString(), out var parsedDate))
            {
                request.DateOfBirth = parsedDate;
            }

            return request;
        }

        private static string? GetValue(IFormCollection form, string key)
        {
            return form.TryGetValue(key, out var values) ? values.ToString() : null;
        }

        private static double? TryParseDouble(IFormCollection form, string key)
        {
            if (!form.TryGetValue(key, out var values))
            {
                return null;
            }

            var raw = values.ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        }

        private static List<string> NormalizePermissions(IEnumerable<string>? permissions)
        {
            if (permissions == null)
            {
                return new List<string>();
            }

            return permissions
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static User MapToUser(CreateUserDto dto)
        {
            var permissions = NormalizePermissions(dto.Account.Permissions);
            if (!permissions.Any())
            {
                permissions = ApplicationPermissions.DefaultUserPermissions.ToList();
            }

            return new User
            {
                Account = new UserAccount
                {
                    Email = dto.Account.Email,
                    Permissions = permissions,
                    DateJoined = dto.Account.DateJoined ?? DateTime.UtcNow
                },
                Profile = new UserProfile
                {
                    Name = dto.Profile.Name,
                    DateOfBirth = dto.Profile.DateOfBirth,
                    ProfilePictureUrl = dto.Profile.ProfilePictureUrl,
                    JobTitle = dto.Profile.JobTitle,
                    Department = dto.Profile.Department,
                    Organization = dto.Profile.Organization,
                    Location = dto.Profile.Location
                }
            };
        }
    }
}
