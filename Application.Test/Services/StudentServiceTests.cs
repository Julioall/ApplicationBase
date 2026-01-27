using Application.Domain.Exceptions;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Students;
using Application.Domain.Model.Students.Dtos;
using Application.Domain.Model.ValueObjects;
using Application.Service.Interface;
using Application.Tests.Setup;
using ClosedXML.Excel;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Application.Tests.Services
{
    public class StudentServiceTests : BaseTest
    {
        private readonly IStudentService _studentService;
        private readonly IStringLocalizer<Application.Domain.SharedResource> _localizer;

        public StudentServiceTests()
        {
            _studentService = _serviceProvider.GetService<IStudentService>()
                ?? throw new Exception($"{nameof(IStudentService)} não foi encontrado");
            _localizer = _serviceProvider.GetService<IStringLocalizer<Application.Domain.SharedResource>>()
                ?? throw new Exception("Localizador não encontrado");
        }

        [Fact]
        public async Task CreateStudent_Should_Persist()
        {
            var dto = CreateStudentDto(idNumber: "123456");
            var expectedLastAccess = DateTime.UtcNow.AddDays(-1);
            dto.LastAccessAt = expectedLastAccess;
            dto.Status = StudentStatus.Suspended;
            dto.IsActive = false;

            var created = await _studentService.CreateStudentAsync(dto);
            await _asyncSession.SaveChangesAsync();

            var loaded = await _studentService.GetStudentAsync(created.Id!);

            Assert.NotNull(loaded);
            Assert.Equal(dto.FirstName, loaded!.FirstName);
            Assert.Equal(dto.LastName, loaded.LastName);
            Assert.False(loaded.IsActive);
            Assert.Equal(StudentStatus.Suspended, loaded.Status);
            Assert.Equal(expectedLastAccess, loaded.LastAccessAt);
            Assert.NotEqual(default, loaded.CreatedAt);
        }

        [Fact]
        public async Task CreateStudent_Should_Throw_For_Duplicate_IdNumber()
        {
            var dto = CreateStudentDto(idNumber: "CPF001");
            await _studentService.CreateStudentAsync(dto);
            await _asyncSession.SaveChangesAsync();

            var duplicate = CreateStudentDto(firstName: "Jane", lastName: "Roe", idNumber: "CPF001");

            await Assert.ThrowsAsync<ConflictException>(() => _studentService.CreateStudentAsync(duplicate));
        }

        [Fact]
        public async Task UpdateStudent_Should_Change_Data()
        {
            var dto = CreateStudentDto(idNumber: "UPD001");
            var created = await _studentService.CreateStudentAsync(dto);
            await _asyncSession.SaveChangesAsync();

            var updateDto = new UpdateStudentDto
            {
                FirstName = "Maria",
                LastName = "Silva",
                Email = "maria@school.com",
                IdNumber = "UPD001",
                Phone = "555-1234",
                Address = new Address
                {
                    Street = "Rua A",
                    Number = "100",
                    City = "Goiânia",
                    State = "GO",
                    PostalCode = "74000-000",
                    Country = "Brasil"
                },
                Institution = "Tech University",
                Lang = "pt_BR",
                TimeZone = "America/Sao_Paulo",
                IsActive = false,
                Status = StudentStatus.NotCurrently,
                LastAccessAt = DateTime.UtcNow.AddDays(-2)
            };

            var updated = await _studentService.UpdateStudentAsync(created.Id!, updateDto);
            await _asyncSession.SaveChangesAsync();

            var loaded = await _studentService.GetStudentAsync(updated.Id!);
            Assert.Equal("Maria", loaded!.FirstName);
            Assert.Equal("Silva", loaded.LastName);
            Assert.Equal("Tech University", loaded.Institution);
            Assert.Equal("America/Sao_Paulo", loaded.TimeZone);
            Assert.Equal(StudentStatus.NotCurrently, loaded.Status);
            Assert.Equal(updateDto.LastAccessAt, loaded.LastAccessAt);
        }

        [Fact]
        public async Task UpdateStudent_Should_Throw_When_NotFound()
        {
            var dto = new UpdateStudentDto
            {
                FirstName = "Ghost",
                LastName = "User",
                IsActive = true
            };

            await Assert.ThrowsAsync<NotFoundException>(() => _studentService.UpdateStudentAsync("students/999-A", dto));
        }

        [Fact]
        public async Task DeleteStudent_Should_Set_Inactive()
        {
            var dto = CreateStudentDto(idNumber: "DEL001");
            var created = await _studentService.CreateStudentAsync(dto);
            await _asyncSession.SaveChangesAsync();

            await _studentService.DeleteStudentAsync(created.Id!);
            await _asyncSession.SaveChangesAsync();

            var loaded = await _studentService.GetStudentAsync(created.Id!);
            Assert.NotNull(loaded);
            Assert.False(loaded!.IsActive);
            Assert.Equal(StudentStatus.NotCurrently, loaded.Status);
        }

        [Fact]
        public async Task CreateStudent_Should_Validate_TimeZone()
        {
            var dto = CreateStudentDto();
            dto.TimeZone = "Invalid/Zone";

            await Assert.ThrowsAsync<ValidationException>(() => _studentService.CreateStudentAsync(dto));
        }

        [Fact]
        public async Task ImportStudents_Should_Create_And_Update_By_Email()
        {
            var existing = await _studentService.CreateStudentAsync(new CreateStudentDto
            {
                FirstName = "Old",
                LastName = "Name",
                Email = "existing@test.com",
                IsActive = true,
                Status = StudentStatus.Active
            });
            await _asyncSession.SaveChangesAsync();

            var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Students");
            ws.Cell(1, 1).Value = "Nome";
            ws.Cell(1, 2).Value = "Sobrenome";
            ws.Cell(1, 3).Value = "Ultimo acesso";
            ws.Cell(1, 4).Value = "Endereco de e-mail";

            ws.Cell(2, 1).Value = "John";
            ws.Cell(2, 2).Value = "Updated";
            ws.Cell(2, 3).Value = DateTime.UtcNow.AddDays(-1);
            ws.Cell(2, 4).Value = "existing@test.com";

            ws.Cell(3, 1).Value = "Alice";
            ws.Cell(3, 2).Value = "New";
            ws.Cell(3, 3).Value = DateTime.UtcNow.AddDays(-2);
            ws.Cell(3, 4).Value = "new@test.com";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var importResult = await _studentService.ImportStudentsAsync(stream, "students.xlsx");
            await _asyncSession.SaveChangesAsync();

            Assert.Equal(2, importResult.Processed);
            Assert.Equal(1, importResult.Updated);
            Assert.Equal(1, importResult.Created);
            Assert.Equal(0, importResult.Skipped);
            Assert.Empty(importResult.Errors);

            var updated = await _studentService.GetStudentAsync(existing.Id!);
            Assert.Equal("John", updated!.FirstName);
            Assert.Equal("Updated", updated.LastName);
            Assert.Equal("existing@test.com", updated.Email);
            Assert.NotNull(updated.LastAccessAt);

            var students = await _studentService.GetStudentsAsync(new PaginationQuery { PageNumber = 1, PageSize = 10 });
            var created = students.Items.FirstOrDefault(s => s.Email == "new@test.com");
            Assert.NotNull(created);
            Assert.Equal("Alice", created!.FirstName);
            Assert.Equal("New", created.LastName);
        }

        [Fact]
        public async Task ImportStudents_Should_Report_Invalid_Date()
        {
            var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Students");
            ws.Cell(1, 1).Value = "Nome";
            ws.Cell(1, 2).Value = "Sobrenome";
            ws.Cell(1, 3).Value = "Ultimo acesso";
            ws.Cell(1, 4).Value = "Endereco de e-mail";

            ws.Cell(2, 1).Value = "John";
            ws.Cell(2, 2).Value = "Doe";
            ws.Cell(2, 3).Value = "not-a-date";
            ws.Cell(2, 4).Value = "john@doe.com";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var importResult = await _studentService.ImportStudentsAsync(stream, "students.xlsx");

            Assert.Equal(1, importResult.Processed);
            Assert.Equal(1, importResult.Skipped);
            Assert.Single(importResult.Errors);
            Assert.Contains(_localizer["StudentImportInvalidDateWithValue", "not-a-date"].Value, importResult.Errors[0].Message);
        }

        [Fact]
        public async Task ExportStudents_Should_Return_Workbook_With_Data()
        {
            await _studentService.CreateStudentAsync(new CreateStudentDto
            {
                FirstName = "Maria",
                LastName = "Silva",
                Email = "maria@test.com",
                IsActive = true,
                Status = StudentStatus.Active,
                LastAccessAt = DateTime.UtcNow.AddDays(-3)
            });
            await _studentService.CreateStudentAsync(new CreateStudentDto
            {
                FirstName = "Ana",
                LastName = "Costa",
                Email = "ana@test.com",
                IsActive = true,
                Status = StudentStatus.Active
            });
            await _asyncSession.SaveChangesAsync();

            var bytes = await _studentService.ExportStudentsAsync();

            using var stream = new MemoryStream(bytes);
            var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();

            Assert.Equal(_localizer["StudentExportHeaderFirstName"].Value, ws.Cell(1, 1).GetString());
            Assert.Equal(_localizer["StudentExportHeaderLastName"].Value, ws.Cell(1, 2).GetString());
            Assert.Equal(_localizer["StudentExportHeaderLastAccess"].Value, ws.Cell(1, 3).GetString());
            Assert.Equal(_localizer["StudentExportHeaderEmail"].Value, ws.Cell(1, 4).GetString());

            // Two students + header
            Assert.Equal(3, ws.LastRowUsed()!.RowNumber());
            var exportedEmails = new[]
            {
                ws.Cell(2, 4).GetString(),
                ws.Cell(3, 4).GetString()
            };
            Assert.Contains("maria@test.com", exportedEmails);
            Assert.Contains("ana@test.com", exportedEmails);
        }

        private static CreateStudentDto CreateStudentDto(string firstName = "John", string lastName = "Doe", string? idNumber = null)
        {
            return new CreateStudentDto
            {
                FirstName = firstName,
                LastName = lastName,
                Email = $"{firstName}.{lastName}@test.com".ToLowerInvariant(),
                IdNumber = idNumber,
                Phone = "123456789",
                Address = new Address
                {
                    Street = "Main St",
                    Number = "1",
                    District = "Central",
                    City = "City",
                    State = "State",
                    PostalCode = "00000-000",
                    Country = "Country"
                },
                Institution = "Test School",
                Lang = "en",
                TimeZone = "America/New_York",
                IsActive = true,
                Status = StudentStatus.Active,
                LastAccessAt = DateTime.UtcNow.AddDays(-1)
            };
        }
    }
}
