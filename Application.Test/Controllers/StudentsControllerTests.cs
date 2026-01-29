using Application.Api.Controllers;
using Application.Domain.Exceptions;
using Application.Domain.Localization;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Students;
using Application.Domain.Model.Students.Dtos;
using Application.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Application.Tests.Controllers
{
    public class StudentsControllerTests
    {
        private sealed class FakeLocalizer : IStringLocalizer<SharedResource>
        {
            public LocalizedString this[string name] => new(name, name);
            public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
            public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
        }

        private sealed class FakeStudentService : IStudentService
        {
            public Func<CreateStudentDto, Task<Student>>? CreateFunc { get; set; }
            public Func<string, Task>? DeleteFunc { get; set; }
            public Func<string, Task<Student?>>? GetFunc { get; set; }
            public Func<PaginationQuery, Task<PagedResult<Student>>>? GetStudentsFunc { get; set; }
            public Func<string, UpdateStudentDto, Task<Student>>? UpdateFunc { get; set; }
            public Func<Task<byte[]>>? ExportFunc { get; set; }

            public Task<Student> CreateStudentAsync(CreateStudentDto dto) => CreateFunc?.Invoke(dto) ?? Task.FromResult(new Student { FirstName = string.Empty, LastName = string.Empty, IsActive = true, CreatedAt = DateTime.UtcNow });

            public Task DeleteStudentAsync(string id) => DeleteFunc?.Invoke(id) ?? Task.CompletedTask;

            public Task<Student?> GetStudentAsync(string id) => GetFunc?.Invoke(id) ?? Task.FromResult<Student?>(null);

            public Task<PagedResult<Student>> GetStudentsAsync(PaginationQuery query) => GetStudentsFunc?.Invoke(query) ?? Task.FromResult(new PagedResult<Student>
            {
                Items = Array.Empty<Student>(),
                Total = 0,
                PageNumber = 1,
                PageSize = 10
            });

            public Task<Student> UpdateStudentAsync(string id, UpdateStudentDto dto) => UpdateFunc?.Invoke(id, dto) ?? Task.FromResult(new Student { FirstName = string.Empty, LastName = string.Empty, IsActive = true, CreatedAt = DateTime.UtcNow });

            public Task<byte[]> ExportStudentsAsync() => ExportFunc?.Invoke() ?? Task.FromResult(Array.Empty<byte>());
        }

        private static StudentsController CreateController(FakeStudentService? service = null, IStringLocalizer<SharedResource>? localizer = null)
        {
            service ??= new FakeStudentService();
            localizer ??= new FakeLocalizer();

            return new StudentsController(service, localizer)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        private static CreateStudentDto CreateDto(string firstName = "John", string lastName = "Doe")
        {
            return new CreateStudentDto
            {
                FirstName = firstName,
                LastName = lastName,
                Email = "john@doe.com",
                IdNumber = "ID123",
                IsActive = true
            };
        }

        [Fact]
        public async Task GetStudents_Should_Return_Ok_With_Result()
        {
            var expected = new PagedResult<Student>
            {
                Items = new List<Student> { new Student { Id = "students/1-A", FirstName = "Ana", LastName = "Silva", IsActive = true, CreatedAt = DateTime.UtcNow } },
                Total = 1,
                PageNumber = 1,
                PageSize = 10
            };

            var service = new FakeStudentService
            {
                GetStudentsFunc = _ => Task.FromResult(expected)
            };
            var controller = CreateController(service);

            var result = await controller.GetStudents(new PaginationQuery());

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(expected, okResult.Value);
        }

        [Fact]
        public async Task GetStudent_Should_Return_Student_When_Found()
        {
            var student = new Student { Id = "students/1-A", FirstName = "John", LastName = "Doe", CreatedAt = DateTime.UtcNow };
            var service = new FakeStudentService
            {
                GetFunc = _ => Task.FromResult<Student?>(student)
            };
            var controller = CreateController(service);

            var response = await controller.GetStudent("students/1-A");

            var okResult = Assert.IsType<OkObjectResult>(response);
            Assert.Same(student, okResult.Value);
        }

        [Fact]
        public async Task GetStudent_Should_Throw_When_NotFound()
        {
            var controller = CreateController(new FakeStudentService());

            await Assert.ThrowsAsync<NotFoundException>(() => controller.GetStudent("students/404-A"));
        }

        [Fact]
        public async Task CreateStudent_Should_Return_Created()
        {
            var dto = CreateDto();
            var created = new Student { Id = "students/2-A", FirstName = dto.FirstName, LastName = dto.LastName, CreatedAt = DateTime.UtcNow };
            var service = new FakeStudentService
            {
                CreateFunc = _ => Task.FromResult(created)
            };
            var controller = CreateController(service);

            var result = await controller.CreateStudent(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal("GetStudent", createdResult.ActionName);
            Assert.Equal(created.Id, ((dynamic?)createdResult.Value)?.Id as string);
        }

        [Fact]
        public async Task CreateStudent_Should_Return_Problem_When_Request_Is_Null()
        {
            var controller = CreateController();

            var result = await controller.CreateStudent(null);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }

        [Fact]
        public async Task UpdateStudent_Should_Return_Ok()
        {
            var dto = new UpdateStudentDto { FirstName = "Jane", LastName = "Roe", IsActive = true };
            var updated = new Student { Id = "students/3-A", FirstName = "Jane", LastName = "Roe", CreatedAt = DateTime.UtcNow };
            var service = new FakeStudentService
            {
                UpdateFunc = (_, _) => Task.FromResult(updated)
            };
            var controller = CreateController(service);

            var result = await controller.UpdateStudent("students/3-A", dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(updated.Id, ((dynamic?)okResult.Value)?.Id as string);
        }

        [Fact]
        public async Task UpdateStudent_Should_Return_Problem_When_Request_Is_Null()
        {
            var controller = CreateController();

            var result = await controller.UpdateStudent("students/3-A", null);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }

        [Fact]
        public async Task DeleteStudent_Should_Return_NoContent()
        {
            var deletedId = string.Empty;
            var service = new FakeStudentService
            {
                DeleteFunc = id =>
                {
                    deletedId = id;
                    return Task.CompletedTask;
                }
            };
            var controller = CreateController(service);

            var result = await controller.DeleteStudent("students/5-A");

            Assert.IsType<NoContentResult>(result);
            Assert.Equal("students/5-A", deletedId);
        }

        [Fact]
        public async Task ExportStudents_Should_Return_File()
        {
            var expectedBytes = new byte[] { 1, 2, 3, 4 };
            var service = new FakeStudentService
            {
                ExportFunc = () => Task.FromResult(expectedBytes)
            };
            var controller = CreateController(service);

            var result = await controller.ExportStudents();

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(expectedBytes, fileResult.FileContents);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
            Assert.StartsWith("students_", fileResult.FileDownloadName);
        }
    }
}
