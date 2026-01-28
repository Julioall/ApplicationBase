using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Moodle;
using Application.Domain.Model.Moodle.Dtos;
using Application.Domain.Model.Students;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MoodleController : ControllerBase
    {
        private readonly IMoodleService _moodleService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public MoodleController(IMoodleService moodleService, IStringLocalizer<SharedResource> localizer)
        {
            _moodleService = moodleService;
            _localizer = localizer;
        }

        [HttpGet("categories")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<MoodleCategory>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        {
            var categories = await _moodleService.GetCategoriesAsync(cancellationToken);
            return Ok(categories);
        }

        [HttpGet("course-categories")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<MoodleCourseCategory>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCourseCategories([FromQuery] string? categoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["MoodleCategoryRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            var courseCategories = await _moodleService.GetCourseCategoriesByCategoryAsync(categoryId, cancellationToken);
            return Ok(courseCategories);
        }

        [HttpGet("cohorts")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<MoodleCohort>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCohorts([FromQuery] string? courseCategoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(courseCategoryId))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["MoodleCourseCategoryRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            var cohorts = await _moodleService.GetCohortsByCourseCategoryAsync(courseCategoryId, cancellationToken);
            return Ok(cohorts);
        }

        [HttpGet("cohorts/{cohortId}/courses")]
        [HttpGet("courses")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<MoodleCourse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourses(
            [FromRoute] string? cohortId,
            [FromQuery(Name = "cohortId")] string? cohortIdQuery,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            cohortId ??= cohortIdQuery;

            if (string.IsNullOrWhiteSpace(cohortId))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["MoodleCohortRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            var courses = await _moodleService.GetCoursesByCohortAsync(cohortId, search, cancellationToken);
            return Ok(courses);
        }

        [HttpGet("courses/search")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(PagedResult<MoodleCourse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchCourses([FromQuery] PaginationQuery? query, [FromQuery] string? cohortId, [FromQuery] string? courseCategoryId, CancellationToken cancellationToken)
        {
            var safeQuery = query ?? new PaginationQuery();
            var paged = await _moodleService.SearchCoursesAsync(safeQuery, cohortId, courseCategoryId, cancellationToken);
            return Ok(paged);
        }

        [HttpGet("students/{studentId}/courses")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<MoodleCourse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentCourses(string studentId, CancellationToken cancellationToken)
        {
            var courses = await _moodleService.GetCoursesByStudentAsync(studentId, cancellationToken);
            return Ok(courses);
        }

        [HttpGet("courses/students")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<StudentCourseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCourseStudents([FromQuery] int eadId, CancellationToken cancellationToken)
        {
            if (eadId <= 0)
            {
                return Problem(
                    title: _localizer["InvalidRequestTitle"],
                    detail: "Invalid Course EadId",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var students = await _moodleService.GetStudentsByCourseEadIdWithPerformanceAsync(eadId, cancellationToken);
            return Ok(students);
        }

        [HttpGet("students/courses/performance")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(StudentCoursePerformance), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentPerformance([FromQuery] string studentId, [FromQuery] string courseId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(courseId))
            {
                return Problem(
                    title: _localizer["InvalidRequestTitle"],
                    detail: _localizer["InvalidRequestDetail"],
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var performance = await _moodleService.GetStudentCoursePerformanceAsync(studentId, courseId, cancellationToken);
            return Ok(performance);
        }

        [HttpPatch("students/activities/toggle-hidden")]
        [Authorize(Policy = ApplicationPermissions.ManageEducation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleActivityHidden([FromQuery] string studentId, [FromQuery] string courseId, [FromQuery] string activityName, CancellationToken cancellationToken)
        {
            return MoodleReadOnlyProblem();
        }

        [HttpGet("sync-status")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(MoodleSyncStatus), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSyncStatus(CancellationToken cancellationToken)
        {
            var status = await _moodleService.GetSyncStatusAsync(cancellationToken);
            return Ok(status);
        }

        [HttpPost("sync/trigger")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(MoodleSyncStatus), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> TriggerSync(CancellationToken cancellationToken)
        {
            var (userId, userName) = GetCurrentUserIdentity();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Problem(
                    title: _localizer["UnauthorizedTitle"],
                    detail: _localizer["UnauthorizedDetail"],
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            try
            {
                var status = await _moodleService.TriggerSyncAsync(userId, userName, cancellationToken);
                return Accepted(status);
            }
            catch (ConflictException ex)
            {
                return Problem(
                    title: _localizer["InvalidOperationTitle"],
                    detail: ex.Message,
                    statusCode: StatusCodes.Status409Conflict);
            }
            catch (TooManyRequestsException ex)
            {
                return Problem(
                    title: _localizer["InvalidOperationTitle"],
                    detail: ex.Message,
                    statusCode: StatusCodes.Status429TooManyRequests);
            }
        }

        private ObjectResult MoodleReadOnlyProblem()
        {
            return Problem(
                title: _localizer["InvalidOperationTitle"],
                detail: _localizer["MoodleReadOnly"],
                statusCode: StatusCodes.Status409Conflict);
        }

        private (string? Id, string? Name) GetCurrentUserIdentity()
        {
            var claimOrder = new[]
            {
                ClaimTypes.NameIdentifier,
                "sub",
                "user_id",
                "id"
            };

            foreach (var claim in claimOrder)
            {
                var value = User.FindFirstValue(claim);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var name = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
                    return (value, name);
                }
            }

            return (null, null);
        }
    }
}
