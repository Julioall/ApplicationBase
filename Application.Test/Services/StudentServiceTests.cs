using Application.Domain.Exceptions;
using Application.Domain.Model.Students.Dtos;
using Application.Domain.Model.ValueObjects;
using Application.Service.Interface;
using Application.Tests.Setup;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Services
{
    public class StudentServiceTests : BaseTest
    {
        private readonly IStudentService _studentService;

        public StudentServiceTests()
        {
            _studentService = _serviceProvider.GetService<IStudentService>()
                ?? throw new Exception($"{nameof(IStudentService)} nǜo foi encontrado");
        }

        [Fact]
        public async Task CreateStudent_Should_Persist()
        {
            var dto = CreateStudentDto(idNumber: "123456");

            var created = await _studentService.CreateStudentAsync(dto);
            await _asyncSession.SaveChangesAsync();

            var loaded = await _studentService.GetStudentAsync(created.Id!);

            Assert.NotNull(loaded);
            Assert.Equal(dto.FirstName, loaded!.FirstName);
            Assert.Equal(dto.LastName, loaded.LastName);
            Assert.True(loaded.IsActive);
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
                DateOfBirth = DateTime.UtcNow.AddYears(-20),
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
                IsActive = true
            };

            var updated = await _studentService.UpdateStudentAsync(created.Id!, updateDto);
            await _asyncSession.SaveChangesAsync();

            var loaded = await _studentService.GetStudentAsync(updated.Id!);
            Assert.Equal("Maria", loaded!.FirstName);
            Assert.Equal("Silva", loaded.LastName);
            Assert.Equal("Tech University", loaded.Institution);
            Assert.Equal("America/Sao_Paulo", loaded.TimeZone);
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
        }

        [Fact]
        public async Task CreateStudent_Should_Validate_TimeZone()
        {
            var dto = CreateStudentDto();
            dto.TimeZone = "Invalid/Zone";

            await Assert.ThrowsAsync<ValidationException>(() => _studentService.CreateStudentAsync(dto));
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
                DateOfBirth = DateTime.UtcNow.AddYears(-18),
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
                IsActive = true
            };
        }
    }
}
