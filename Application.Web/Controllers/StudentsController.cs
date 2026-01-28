using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Students;
using Application.Domain.Model.Students.Dtos;
using Application.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Application.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public StudentsController(IStudentService studentService, IStringLocalizer<SharedResource> localizer)
        {
            _studentService = studentService;
            _localizer = localizer;
        }

        [HttpGet]
        [Authorize(Policy = ApplicationPermissions.ViewStudents)]
        [ProducesResponseType(typeof(PagedResult<Student>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudents([FromQuery] PaginationQuery? query)
        {
            var students = await _studentService.GetStudentsAsync(query ?? new PaginationQuery());
            return Ok(students);
        }

        [HttpGet("export")]
        [Authorize(Policy = ApplicationPermissions.ManageStudents)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportStudents()
        {
            var content = await _studentService.ExportStudentsAsync();
            var fileName = $"students_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = ApplicationPermissions.ViewStudents)]
        [ProducesResponseType(typeof(Student), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStudent(string id)
        {
            var student = await _studentService.GetStudentAsync(id);
            if (student == null)
            {
                throw new NotFoundException(_localizer["StudentNotFound", id]);
            }

            return Ok(student);
        }

        [HttpPost]
        [Authorize(Policy = ApplicationPermissions.ManageStudents)]
        [ProducesResponseType(typeof(Student), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto? dto)
        {
            if (dto == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRequestDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var student = await _studentService.CreateStudentAsync(dto);
            return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, new { student.Id, message = _localizer["StudentCreated"] });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = ApplicationPermissions.ManageStudents)]
        [ProducesResponseType(typeof(Student), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStudent(string id, [FromBody] UpdateStudentDto? dto)
        {
            if (dto == null)
            {
                return Problem(title: _localizer["InvalidRequestTitle"], detail: _localizer["InvalidRequestDetail"], statusCode: StatusCodes.Status400BadRequest);
            }

            var updated = await _studentService.UpdateStudentAsync(id, dto);
            return Ok(new { updated.Id, message = _localizer["StudentUpdated"] });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = ApplicationPermissions.ManageStudents)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteStudent(string id)
        {
            await _studentService.DeleteStudentAsync(id);
            return NoContent();
        }
    }
}
