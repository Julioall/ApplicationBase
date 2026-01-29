using Application.Api.Models.User;
using Application.Api.RateLimiting;
using Application.Domain.Exceptions;
using Application.Domain.Localization;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
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
        private readonly IRateLimiter _rateLimiter;
        private readonly RateLimitSettings _rateLimitSettings;

        public UserController(
            IUserService userService,
            IStringLocalizer<SharedResource> localizer,
            IRateLimiter rateLimiter,
            IOptions<RateLimitSettings> rateLimitSettings)
        {
            _userService = userService;
            _localizer = localizer;
            _rateLimiter = rateLimiter;
            _rateLimitSettings = rateLimitSettings.Value;
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

            if (request.Account == null || request.Profile == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(request.Account.Password))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["PasswordRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            var account = request.Account;
            var rateLimitResult = EnforceRateLimit(
                "register",
                account.Email,
                _rateLimitSettings.RegistrationPerIpLimit,
                _rateLimitSettings.RegistrationWindow,
                _rateLimitSettings.RegistrationPerEmailLimit,
                _rateLimitSettings.RegistrationWindow);
            if (rateLimitResult != null)
            {
                return rateLimitResult;
            }

            var user = MapToUser(request);

            await _userService.AddAsync(user, account.Password);
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

            if (IsMoodleUser())
            {
                var subject = GetSubject();
                var moodleId = GetMoodleId(subject);
                var username = GetUsernameFromClaims() ?? email;
                var picture = GetPictureFromClaims();
                var department = GetClaimValue("department");
                var organization = GetClaimValue("organization");
                var city = GetClaimValue("city");
                var country = GetClaimValue("country");
                var location = BuildLocation(city, country);
                var jobTitle = department ?? organization ?? username;
                var permissions = GetPermissionsFromClaims();
                if (!permissions.Any())
                {
                    permissions = ApplicationPermissions.DefaultUserPermissions.ToList();
                }

                var displayName = GetNameFromClaims() ?? email;

                var virtualUser = new User
                {
                    Id = subject,
                        Account = new UserAccount
                    {
                        Email = email,
                        Username = username,
                        MoodleId = GetClaimValue("idnumber") ?? moodleId,
                        Permissions = permissions,
                        DateJoined = DateTime.UtcNow
                    },
                    Profile = new UserProfile
                    {
                        Name = displayName,
                        ProfilePictureUrl = picture,
                        JobTitle = jobTitle,
                        Department = department ?? organization ?? username,
                        Organization = organization ?? department ?? username,
                        Location = location
                    }
                };

                return Ok(virtualUser);
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

            var userId = user.Id;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var existingUser = await _userService.GetByIdAsync(userId);
            if (existingUser == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundById", userId]);
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
            if (IsMoodleUser())
            {
                return MoodleActionNotAllowed();
            }

            var email = GetAuthenticatedEmail();
            if (string.IsNullOrWhiteSpace(email))
            {
                return Problem(title: _localizer["UnauthorizedTitle"], detail: _localizer["UnauthorizedDetail"], statusCode: StatusCodes.Status401Unauthorized);
            }

            Stream? profilePictureStream = null;
            string? contentType = null;
            string? name = null;
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
                            return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["ProfilePictureContentTypeInvalid"], statusCode: StatusCodes.Status400BadRequest);
                        }
                        if (formRequest.ProfilePicture.Length > MaxAvatarBytes)
                        {
                            return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["ProfilePictureTooLarge"], statusCode: StatusCodes.Status400BadRequest);
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
                            return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["ProfilePictureTooLarge"], statusCode: StatusCodes.Status400BadRequest);
                        }
                        if (!dtoContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        {
                            return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["ProfilePictureContentTypeInvalid"], statusCode: StatusCodes.Status400BadRequest);
                        }
                        profilePictureStream = new MemoryStream(data);
                        contentType = dtoContentType;
                    }
                }

                await _userService.UpdateProfileAsync(
                    email,
                    name,
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
            if (IsMoodleUser())
            {
                return MoodleActionNotAllowed();
            }

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

        [AllowAnonymous]
        [HttpPost("recovery/code")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateRecoveryCode([FromBody] GenerateRecoveryCodeDto? request)
        {
            if (IsMoodleUser())
            {
                return MoodleActionNotAllowed();
            }

            var email = request?.Email;
            email ??= GetAuthenticatedEmail();
            if (string.IsNullOrWhiteSpace(email))
            {
                return Problem(title: _localizer["UnauthorizedTitle"], detail: _localizer["UnauthorizedDetail"], statusCode: StatusCodes.Status401Unauthorized);
            }

            var rateLimitResult = EnforceRateLimit(
                "recovery-generate",
                email,
                _rateLimitSettings.RecoveryGeneratePerIpLimit,
                _rateLimitSettings.RecoveryGenerateWindow,
                _rateLimitSettings.RecoveryGeneratePerEmailLimit,
                _rateLimitSettings.RecoveryGenerateWindow);
            if (rateLimitResult != null)
            {
                return rateLimitResult;
            }

            var sendEmail = request?.SendEmail ?? true;
            try
            {
                var (code, expiresAt) = await _userService.GenerateRecoveryCodeAsync(email, sendEmail);
                if (string.IsNullOrWhiteSpace(code))
                {
                    return Ok(new { message = _localizer["ResetEmailSent"], sentByEmail = sendEmail });
                }
                return Ok(new { expiresAt, sentByEmail = sendEmail });
            }
            catch (NotFoundException)
            {
                return Ok(new { message = _localizer["ResetEmailSent"], sentByEmail = sendEmail });
            }
            catch (BusinessException ex)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (InvalidOperationException)
            {
                return Problem(title: _localizer["InternalErrorTitle"], detail: _localizer["EmailNotConfigured"], statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [AllowAnonymous]
        [HttpPost("recovery/validate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidateRecoveryCode([FromBody] ValidateRecoveryCodeDto? request)
        {
            if (IsMoodleUser())
            {
                return MoodleActionNotAllowed();
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Code))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
            }
            if (request.Code.Length != 6 || !request.Code.All(char.IsDigit))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["RecoveryCodeInvalid"], statusCode: StatusCodes.Status400BadRequest);
            }

            var email = request.Email;
            email ??= GetAuthenticatedEmail();
            if (string.IsNullOrWhiteSpace(email))
            {
                return Problem(title: _localizer["UnauthorizedTitle"], detail: _localizer["UnauthorizedDetail"], statusCode: StatusCodes.Status401Unauthorized);
            }

            await _userService.ValidateRecoveryCodeAsync(email, request.Code);
            return Ok(new { message = _localizer["RecoveryCodeValidated"] });
        }

        [AllowAnonymous]
        [HttpPost("recovery/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyRecoveryCode([FromBody] VerifyRecoveryCodeDto? request)
        {
            if (IsMoodleUser())
            {
                return MoodleActionNotAllowed();
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["UserCannotBeNullDetail"], statusCode: StatusCodes.Status400BadRequest);
            }
            if (request.Code.Length != 6 || !request.Code.All(char.IsDigit))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["RecoveryCodeInvalid"], statusCode: StatusCodes.Status400BadRequest);
            }

            var email = request.Email;
            email ??= GetAuthenticatedEmail();
            if (string.IsNullOrWhiteSpace(email))
            {
                return Problem(title: _localizer["UnauthorizedTitle"], detail: _localizer["UnauthorizedDetail"], statusCode: StatusCodes.Status401Unauthorized);
            }

            var rateLimitResult = EnforceRateLimit(
                "recovery-verify",
                email,
                _rateLimitSettings.RecoveryVerifyPerIpLimit,
                _rateLimitSettings.RecoveryVerifyWindow,
                _rateLimitSettings.RecoveryVerifyPerEmailLimit,
                _rateLimitSettings.RecoveryVerifyWindow);
            if (rateLimitResult != null)
            {
                return rateLimitResult;
            }

            await _userService.ChangePasswordWithRecoveryCodeAsync(email, request.Code, request.NewPassword);
            return Ok(new { message = _localizer["UserPasswordUpdatedSuccessfully"] });
        }

        private ActionResult? EnforceRateLimit(string scenarioKey, string? email, int perIpLimit, TimeSpan ipWindow, int perEmailLimit, TimeSpan emailWindow)
        {
            var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
            var clientIp = GetClientIp();

            if (!string.IsNullOrWhiteSpace(clientIp) && perIpLimit > 0)
            {
                if (!_rateLimiter.TryConsume($"{scenarioKey}:ip:{clientIp}", perIpLimit, ipWindow, out var retryAfter))
                {
                    return BuildRateLimitResponse(retryAfter);
                }
            }

            if (!string.IsNullOrWhiteSpace(normalizedEmail) && perEmailLimit > 0)
            {
                if (!_rateLimiter.TryConsume($"{scenarioKey}:email:{normalizedEmail}", perEmailLimit, emailWindow, out var retryAfter))
                {
                    return BuildRateLimitResponse(retryAfter);
                }
            }

            return null;

            ActionResult BuildRateLimitResponse(TimeSpan? retryAfter)
            {
                if (retryAfter.HasValue)
                {
                    Response.Headers["Retry-After"] = Math.Ceiling(retryAfter.Value.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                var detail = retryAfter.HasValue
                    ? _localizer["RateLimitExceededDetail", Math.Ceiling(retryAfter.Value.TotalSeconds)]
                    : _localizer["RateLimitExceededGeneric"];

                return Problem(title: _localizer["RateLimitExceededTitle"], detail: detail, statusCode: StatusCodes.Status429TooManyRequests);
            }
        }

        private string? GetClientIp()
        {
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var first = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(first))
                {
                    return first.Trim();
                }
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private async Task PopulateProfilePictureAsync(User user)
        {
            if (string.IsNullOrWhiteSpace(user?.Id))
            {
                return;
            }

            var userId = user.Id!;
            var attachment = await _userService.GetProfilePictureAsync(userId);
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

        private string? GetNameFromClaims()
        {
            var claimOrder = new[] { "name", ClaimTypes.Name, JwtRegisteredClaimNames.Name, JwtRegisteredClaimNames.UniqueName };
            foreach (var claimType in claimOrder)
            {
                var value = User.FindFirstValue(claimType);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private string? GetUsernameFromClaims()
        {
            var claimOrder = new[] { "username", ClaimTypes.Upn, ClaimTypes.Name, JwtRegisteredClaimNames.UniqueName };
            foreach (var claimType in claimOrder)
            {
                var value = User.FindFirstValue(claimType);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private string? GetPictureFromClaims()
        {
            var value = User.FindFirstValue("picture");
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private string? GetClaimValue(string type)
        {
            var value = User.FindFirstValue(type);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string? BuildLocation(string? city, string? country)
        {
            if (string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(country))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(country))
            {
                return $"{city}, {country}";
            }

            return city ?? country;
        }

        private bool IsMoodleUser()
        {
            var provider = User.FindFirstValue("auth_provider");
            return !string.IsNullOrWhiteSpace(provider) && provider.Equals("moodle", StringComparison.OrdinalIgnoreCase);
        }

        private string? GetSubject()
        {
            return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue(ClaimTypes.Name);
        }

        private static string? GetMoodleId(string? subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                return null;
            }

            const string prefix = "moodle:";
            return subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? subject[prefix.Length..]
                : subject;
        }

        private List<string> GetPermissionsFromClaims()
        {
            return User.FindAll(ApplicationPermissions.PermissionClaimType)
                .Select(c => c.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private ActionResult MoodleActionNotAllowed()
        {
            return Problem(title: _localizer["InvalidOperationTitle"], detail: _localizer["MoodleActionNotAllowed"], statusCode: StatusCodes.Status403Forbidden);
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
