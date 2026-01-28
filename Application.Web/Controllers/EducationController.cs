using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education;
using Application.Domain.Model.Education.Dtos;
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
    public class EducationController : ControllerBase
    {
        private readonly IEducationService _educationService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public EducationController(IEducationService educationService, IStringLocalizer<SharedResource> localizer)
        {
            _educationService = educationService;
            _localizer = localizer;
        }

        [HttpPost("import")]
        [Authorize(Policy = ApplicationPermissions.ManageEducation)]
        [ProducesResponseType(typeof(EducationImport), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Import([FromForm] IFormFile? file, CancellationToken cancellationToken)
        {
            return EducationReadOnlyProblem();
        }

        [HttpGet("import/{id}")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(EducationImport), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetImport(string id, CancellationToken cancellationToken)
        {
            var import = await _educationService.GetImportAsync(id, cancellationToken);
            if (import == null)
            {
                throw new NotFoundException(_localizer["ImportNotFound", id]);
            }

            return Ok(import);
        }

        [HttpGet("schools")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<School>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchools(CancellationToken cancellationToken)
        {
            var schools = await _educationService.GetSchoolsAsync(cancellationToken);
            return Ok(schools);
        }

        [HttpGet("programs")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<ProgramDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPrograms([FromQuery] string? schoolId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(schoolId))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["EducationSchoolRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            var programs = await _educationService.GetProgramsBySchoolAsync(schoolId, cancellationToken);
            return Ok(programs);
        }

        [HttpGet("classes")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<ClassDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetClasses([FromQuery] string? programId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(programId))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["EducationProgramRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            var classes = await _educationService.GetClassesByProgramAsync(programId, cancellationToken);
            return Ok(classes);
        }

        [HttpGet("classes/{classId}/ucs")]
        [HttpGet("ucs")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<UcDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUcs(
            [FromRoute] string? classId,
            [FromQuery(Name = "classId")] string? classIdQuery,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            classId ??= classIdQuery;

            if (string.IsNullOrWhiteSpace(classId))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["EducationClassRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            var ucs = await _educationService.GetUcsByClassAsync(classId, search, cancellationToken);
            return Ok(ucs);
        }

        [HttpGet("ucs/search")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(PagedResult<UcDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchUcs([FromQuery] PaginationQuery? query, [FromQuery] string? classId, [FromQuery] string? programId, CancellationToken cancellationToken)
        {
            var safeQuery = query ?? new PaginationQuery();
            var paged = await _educationService.SearchUcsAsync(safeQuery, classId, programId, cancellationToken);
            return Ok(paged);
        }

        [HttpPost("import-report")]
        [Authorize(Policy = ApplicationPermissions.ManageEducation)]
        [ProducesResponseType(typeof(EducationReportImport), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ImportReport([FromForm] IFormFileCollection files, CancellationToken cancellationToken)
        {
            return EducationReadOnlyProblem();
        }

        [HttpGet("students/{studentId}/ucs")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<UcDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentUcs(string studentId, CancellationToken cancellationToken)
        {
            var ucs = await _educationService.GetUcsByStudentAsync(studentId, cancellationToken);
            return Ok(ucs);
        }

        [HttpGet("ucs/students")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<StudentUcDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUcStudents([FromQuery] int eadId, CancellationToken cancellationToken)
        {
            if (eadId <= 0)
            {
                return Problem(
                    title: _localizer["InvalidRequestTitle"],
                    detail: "Invalid UC EadId",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var students = await _educationService.GetStudentsByUcEadIdWithPerformanceAsync(eadId, cancellationToken);
            return Ok(students);
        }

        [HttpGet("students/ucs/performance")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(StudentUcPerformance), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentPerformance([FromQuery] string studentId, [FromQuery] string ucId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(ucId))
            {
                return Problem(
                    title: _localizer["InvalidRequestTitle"],
                    detail: _localizer["InvalidRequestDetail"],
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var performance = await _educationService.GetStudentUcPerformanceAsync(studentId, ucId, cancellationToken);
            return Ok(performance);
        }

        [HttpPatch("students/activities/toggle-hidden")]
        [Authorize(Policy = ApplicationPermissions.ManageEducation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleActivityHidden([FromQuery] string studentId, [FromQuery] string ucId, [FromQuery] string activityName, CancellationToken cancellationToken)
        {
            return EducationReadOnlyProblem();
        }

        [HttpGet("sync-status")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(EducationSyncStatus), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSyncStatus(CancellationToken cancellationToken)
        {
            var status = await _educationService.GetSyncStatusAsync(cancellationToken);
            return Ok(status);
        }

        [HttpPost("sync/trigger")]
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(EducationSyncStatus), StatusCodes.Status202Accepted)]
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
                var status = await _educationService.TriggerSyncAsync(userId, userName, cancellationToken);
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

        private ObjectResult EducationReadOnlyProblem()
        {
            return Problem(
                title: _localizer["InvalidOperationTitle"],
                detail: _localizer["EducationReadOnly"],
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
