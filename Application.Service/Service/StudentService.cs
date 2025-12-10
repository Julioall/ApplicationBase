using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface.Students;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Students;
using Application.Domain.Model.Students.Dtos;
using Application.Domain.Model.ValueObjects;
using Application.Service.Interface;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Service.Service
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IValidator<Student> _studentValidator;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public StudentService(IStudentRepository studentRepository, IValidator<Student> studentValidator, IStringLocalizer<SharedResource> localizer)
        {
            _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
            _studentValidator = studentValidator ?? throw new ArgumentNullException(nameof(studentValidator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Student> CreateStudentAsync(CreateStudentDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentException.ThrowIfNullOrWhiteSpace(dto.FirstName);
            ArgumentException.ThrowIfNullOrWhiteSpace(dto.LastName);

            var student = new Student
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = Normalize(dto.Email),
                IdNumber = Normalize(dto.IdNumber),
                Phone = Normalize(dto.Phone),
                DateOfBirth = dto.DateOfBirth,
                Address = CloneAddress(dto.Address),
                Institution = Normalize(dto.Institution),
                Lang = Normalize(dto.Lang),
                TimeZone = Normalize(dto.TimeZone),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await ValidateAsync(student, null);
            await _studentRepository.CreateAsync(student);

            return student;
        }

        public async Task<Student> UpdateStudentAsync(string id, UpdateStudentDto dto)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentException.ThrowIfNullOrWhiteSpace(dto.FirstName);
            ArgumentException.ThrowIfNullOrWhiteSpace(dto.LastName);

            var existing = await _studentRepository.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException(_localizer["StudentNotFound", id]);
            }

            existing.FirstName = dto.FirstName.Trim();
            existing.LastName = dto.LastName.Trim();
            existing.Email = Normalize(dto.Email);
            existing.IdNumber = Normalize(dto.IdNumber);
            existing.Phone = Normalize(dto.Phone);
            existing.DateOfBirth = dto.DateOfBirth;
            existing.Address = CloneAddress(dto.Address);
            existing.Institution = Normalize(dto.Institution);
            existing.Lang = Normalize(dto.Lang);
            existing.TimeZone = Normalize(dto.TimeZone);
            existing.IsActive = dto.IsActive;
            existing.CreatedAt = existing.CreatedAt == default ? DateTime.UtcNow : existing.CreatedAt;
            existing.UpdatedAt = DateTime.UtcNow;

            await ValidateAsync(existing, existing.Id);
            await _studentRepository.UpdateAsync(existing);

            return existing;
        }

        public async Task DeleteStudentAsync(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            var existing = await _studentRepository.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException(_localizer["StudentNotFound", id]);
            }

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;

            await _studentRepository.UpdateAsync(existing);
        }

        public Task<Student?> GetStudentAsync(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return _studentRepository.GetByIdAsync(id);
        }

        public Task<PagedResult<Student>> GetStudentsAsync(PaginationQuery query)
        {
            var normalizedQuery = NormalizeQuery(query);
            return _studentRepository.GetPagedAsync(normalizedQuery);
        }

        private async Task ValidateAsync(Student student, string? currentId)
        {
            await _studentValidator.ValidateAndThrowAsync(student);

            if (!string.IsNullOrWhiteSpace(student.IdNumber))
            {
                var exists = await _studentRepository.ExistsByIdNumberAsync(student.IdNumber, currentId);
                if (exists)
                {
                    throw new ConflictException(_localizer["StudentIdNumberExists", student.IdNumber]);
                }
            }
        }

        private static PaginationQuery NormalizeQuery(PaginationQuery query)
        {
            query ??= new PaginationQuery();
            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var search = Normalize(query.Search);

            return new PaginationQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                IsActive = query.IsActive
            };
        }

        private static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        private static Address? CloneAddress(Address? address)
        {
            if (address == null)
            {
                return null;
            }

            return new Address
            {
                Street = Normalize(address.Street),
                Number = Normalize(address.Number),
                District = Normalize(address.District),
                City = Normalize(address.City),
                State = Normalize(address.State),
                PostalCode = Normalize(address.PostalCode),
                Country = Normalize(address.Country)
            };
        }
    }
}
