using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

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
            if (file == null || file.Length == 0)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["CourseImportFileEmpty"], statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var import = await _educationService.EnqueueImportAsync(stream, file.FileName, cancellationToken);
                return Accepted(new { import.Id, import.Status, import.FileName, import.CreatedAt });
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is BusinessException)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
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
        [Authorize(Policy = ApplicationPermissions.ViewEducation)]
        [ProducesResponseType(typeof(IReadOnlyCollection<UcDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUcs(string classId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(classId))
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["EducationClassRequired"], statusCode: StatusCodes.Status400BadRequest);
            }

            var ucs = await _educationService.GetUcsByClassAsync(classId, cancellationToken);
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
    }
}
