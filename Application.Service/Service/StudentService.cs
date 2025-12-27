using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface.Students;
using Application.Domain.Interface.Education;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Students;
using Application.Domain.Model.Students.Dtos;
using Application.Domain.Model.ValueObjects;
using Application.Domain.Model.Education;
using Application.Service.Interface;
using ClosedXML.Excel;
using FluentValidation;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;

namespace Application.Service.Service
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IEducationRepository _educationRepository;
        private readonly IValidator<Student> _studentValidator;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private static readonly Regex CourseIdRegex = new(@"courseid_(\d+)_participants", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public StudentService(IStudentRepository studentRepository, IEducationRepository educationRepository, IValidator<Student> studentValidator, IStringLocalizer<SharedResource> localizer)
        {
            _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
            _educationRepository = educationRepository ?? throw new ArgumentNullException(nameof(educationRepository));
            _studentValidator = studentValidator ?? throw new ArgumentNullException(nameof(studentValidator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Student> CreateStudentAsync(CreateStudentDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentException.ThrowIfNullOrWhiteSpace(dto.FirstName);
            ArgumentException.ThrowIfNullOrWhiteSpace(dto.LastName);

            var status = NormalizeStatus(dto.Status ?? (dto.IsActive ? StudentStatus.Active : StudentStatus.NotCurrently));

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
                Status = status,
                IsActive = status == StudentStatus.Active,
                LastAccessAt = dto.LastAccessAt,
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

            var status = NormalizeStatus(dto.Status ?? existing.Status ?? (dto.IsActive ? StudentStatus.Active : StudentStatus.NotCurrently));

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
            existing.Status = status;
            existing.IsActive = status == StudentStatus.Active;
            existing.CreatedAt = existing.CreatedAt == default ? DateTime.UtcNow : existing.CreatedAt;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.LastAccessAt = dto.LastAccessAt;

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

            existing.Status = StudentStatus.NotCurrently;
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

        public async Task<StudentImportResult> ImportStudentsAsync(Stream fileStream, string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileStream);

            if (fileStream.Length == 0)
            {
                throw new BusinessException(_localizer["StudentImportFileEmpty"]);
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new BusinessException(_localizer["StudentImportOnlyXlsx"]);
            }

            var extension = Path.GetExtension(fileName);
            if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
            {
                return await ImportParticipantsAsync(fileStream, fileName);
            }

            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(_localizer["StudentImportOnlyXlsx"]);
            }

            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                throw new BusinessException(_localizer["StudentImportWorksheetMissing"]);
            }

            var firstRow = worksheet.FirstRowUsed();
            var lastRow = worksheet.LastRowUsed();
            if (firstRow == null || lastRow == null || lastRow.RowNumber() <= firstRow.RowNumber())
            {
                return new StudentImportResult();
            }

            var result = new StudentImportResult();
            var existing = (await _studentRepository.GetAllAsync()).ToList();
            var existingByEmail = existing
                .Where(s => !string.IsNullOrWhiteSpace(s.Email))
                .ToDictionary(s => s.Email!.Trim().ToLowerInvariant(), s => s);

            var startRow = firstRow.RowNumber() + 1;
            for (var rowNumber = startRow; rowNumber <= lastRow.RowNumber(); rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                if (IsRowEmpty(row))
                {
                    continue;
                }

                result.Processed++;

                var firstName = row.Cell(1).GetString().Trim();
                var lastName = row.Cell(2).GetString().Trim();
                var email = row.Cell(4).GetString().Trim();

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    result.Skipped++;
                    result.Errors.Add(new StudentImportError
                    {
                        Row = rowNumber,
                        Message = _localizer["StudentImportNamesRequired"]
                    });
                    continue;
                }

                if (!TryParseDateCell(row.Cell(3), out var lastAccessAt, out var dateError))
                {
                    result.Skipped++;
                    result.Errors.Add(new StudentImportError
                    {
                        Row = rowNumber,
                        Message = dateError ?? _localizer["StudentImportInvalidDate"]
                    });
                    continue;
                }

                try
                {
                    Student? existingStudent = null;
                    var emailKey = string.IsNullOrWhiteSpace(email) ? null : email.ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(emailKey))
                    {
                        existingByEmail.TryGetValue(emailKey!, out existingStudent);
                    }

                    if (existingStudent == null)
                    {
                        var dto = new CreateStudentDto
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            Email = string.IsNullOrWhiteSpace(email) ? null : email,
                            IsActive = true,
                            Status = StudentStatus.Active,
                            LastAccessAt = EnsureUtc(lastAccessAt)
                        };

                        var created = await CreateStudentAsync(dto);
                        if (!string.IsNullOrWhiteSpace(emailKey))
                        {
                            existingByEmail[emailKey!] = created;
                        }
                        result.Created++;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(existingStudent.Id))
                        {
                            result.Skipped++;
                            result.Errors.Add(new StudentImportError
                            {
                                Row = rowNumber,
                                Message = _localizer["StudentImportMissingId"]
                            });
                            continue;
                        }

                        var updateDto = new UpdateStudentDto
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            Email = string.IsNullOrWhiteSpace(email) ? existingStudent.Email : email,
                            IdNumber = existingStudent.IdNumber,
                            Phone = existingStudent.Phone,
                            DateOfBirth = existingStudent.DateOfBirth,
                            Address = CloneAddress(existingStudent.Address),
                            Institution = existingStudent.Institution,
                            Lang = existingStudent.Lang,
                            TimeZone = existingStudent.TimeZone,
                            IsActive = existingStudent.IsActive,
                            Status = existingStudent.Status ?? StudentStatus.Active,
                            LastAccessAt = EnsureUtc(lastAccessAt ?? existingStudent.LastAccessAt)
                        };

                        await UpdateStudentAsync(existingStudent.Id!, updateDto);
                        result.Updated++;
                    }
                }
                catch (Exception ex)
                {
                    result.Skipped++;
                    result.Errors.Add(new StudentImportError
                    {
                        Row = rowNumber,
                        Message = ex.Message
                    });
                }
            }

            return result;
        }

        private async Task<StudentImportResult> ImportParticipantsAsync(Stream fileStream, string fileName)
        {
            var courseId = ExtractCourseId(fileName);
            var uc = await _educationRepository.GetUcByEadIdAsync(courseId);

            var participants = await LoadParticipantsAsync(fileStream);
            if (participants.Count == 0)
            {
                return new StudentImportResult();
            }

            var result = new StudentImportResult();
            var existing = (await _studentRepository.GetAllAsync()).ToList();
            var existingByEmail = existing
                .Where(s => !string.IsNullOrWhiteSpace(s.Email))
                .ToDictionary(s => s.Email!.Trim().ToLowerInvariant(), s => s);

            var rowNumber = 0;
            foreach (var participant in participants)
            {
                rowNumber++;
                result.Processed++;

                var firstName = participant.FirstName?.Trim() ?? string.Empty;
                var lastName = participant.LastName?.Trim() ?? string.Empty;
                var email = participant.Email?.Trim();

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    result.Skipped++;
                    result.Errors.Add(new StudentImportError
                    {
                        Row = rowNumber,
                        Message = _localizer["StudentImportNamesRequired"]
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    result.Skipped++;
                    result.Errors.Add(new StudentImportError
                    {
                        Row = rowNumber,
                        Message = _localizer["StudentImportEmailRequired"]
                    });
                    continue;
                }

                if (!TryParseDateText(participant.LastAccessAt, out var lastAccessAt, out var dateError))
                {
                    result.Skipped++;
                    result.Errors.Add(new StudentImportError
                    {
                        Row = rowNumber,
                        Message = dateError ?? _localizer["StudentImportInvalidDate"]
                    });
                    continue;
                }

                try
                {
                    Student? existingStudent = null;
                    var emailKey = email.ToLowerInvariant();
                    existingByEmail.TryGetValue(emailKey, out existingStudent);

                    if (existingStudent == null)
                    {
                        var dto = new CreateStudentDto
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            Email = email,
                            IsActive = true,
                            Status = StudentStatus.Active,
                            LastAccessAt = EnsureUtc(lastAccessAt)
                        };

                        var created = await CreateStudentAsync(dto);
                        existingByEmail[emailKey] = created;
                        result.Created++;

                        await EnsureStudentUcLinkAsync(created.Id, uc);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(existingStudent.Id))
                        {
                            result.Skipped++;
                            result.Errors.Add(new StudentImportError
                            {
                                Row = rowNumber,
                                Message = _localizer["StudentImportMissingId"]
                            });
                            continue;
                        }

                        var updateDto = new UpdateStudentDto
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            Email = email,
                            IdNumber = existingStudent.IdNumber,
                            Phone = existingStudent.Phone,
                            DateOfBirth = existingStudent.DateOfBirth,
                            Address = CloneAddress(existingStudent.Address),
                            Institution = existingStudent.Institution,
                            Lang = existingStudent.Lang,
                            TimeZone = existingStudent.TimeZone,
                            IsActive = existingStudent.IsActive,
                            Status = existingStudent.Status ?? StudentStatus.Active,
                            LastAccessAt = EnsureUtc(lastAccessAt ?? existingStudent.LastAccessAt)
                        };

                        await UpdateStudentAsync(existingStudent.Id!, updateDto);
                        result.Updated++;

                        await EnsureStudentUcLinkAsync(existingStudent.Id, uc);
                    }
                }
                catch (Exception ex)
                {
                    result.Skipped++;
                    result.Errors.Add(new StudentImportError
                    {
                        Row = rowNumber,
                        Message = ex.Message
                    });
                }
            }

            return result;
        }

        public async Task<byte[]> ExportStudentsAsync()
        {
            var students = (await _studentRepository.GetAllAsync())
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Students");

            worksheet.Cell(1, 1).Value = _localizer["StudentExportHeaderFirstName"].Value;
            worksheet.Cell(1, 2).Value = _localizer["StudentExportHeaderLastName"].Value;
            worksheet.Cell(1, 3).Value = _localizer["StudentExportHeaderLastAccess"].Value;
            worksheet.Cell(1, 4).Value = _localizer["StudentExportHeaderEmail"].Value;
            worksheet.Row(1).Style.Font.Bold = true;

            var currentRow = 2;
            foreach (var student in students)
            {
                worksheet.Cell(currentRow, 1).Value = student.FirstName;
                worksheet.Cell(currentRow, 2).Value = student.LastName;
                worksheet.Cell(currentRow, 3).Value = EnsureUtc(student.LastAccessAt);
                worksheet.Cell(currentRow, 3).Style.DateFormat.Format = "yyyy-mm-dd HH:mm";
                worksheet.Cell(currentRow, 4).Value = student.Email ?? string.Empty;
                currentRow++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
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

        private static DateTime? EnsureUtc(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            if (value.Value.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
            }

            return value.Value.ToUniversalTime();
        }

        private static string NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return StudentStatus.Active;
            }

            var normalized = status.Trim().ToLowerInvariant();
            if (normalized == "inactive" || normalized == "inativo")
            {
                return StudentStatus.NotCurrently;
            }

            return StudentStatus.All.Contains(normalized) ? normalized : normalized;
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

        private static bool IsRowEmpty(IXLRow row)
        {
            for (var col = 1; col <= 4; col++)
            {
                if (!row.Cell(col).IsEmpty())
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryParseDateCell(IXLCell cell, out DateTime? value, out string? error)
        {
            value = null;
            error = null;

            if (cell.IsEmpty())
            {
                return true;
            }

            if (cell.TryGetValue(out DateTime dateValue))
            {
                value = EnsureUtc(dateValue);
                return true;
            }

            var text = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed) ||
                DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
            {
                value = EnsureUtc(parsed);
                return true;
            }

            error = _localizer["StudentImportInvalidDateWithValue", text];
            return false;
        }

        private bool TryParseDateText(string? text, out DateTime? value, out string? error)
        {
            value = null;
            error = null;

            var trimmed = text?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return true;
            }

            var formats = new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss" };
            if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed) ||
                DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed) ||
                DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
            {
                value = EnsureUtc(parsed);
                return true;
            }

            error = _localizer["StudentImportInvalidDateWithValue", trimmed];
            return false;
        }

        private int ExtractCourseId(string fileName)
        {
            var name = Path.GetFileName(fileName);
            var match = CourseIdRegex.Match(name ?? string.Empty);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var courseId))
            {
                throw new BusinessException(_localizer["StudentImportCourseIdMissing"]);
            }

            return courseId;
        }

        private async Task<List<Participant>> LoadParticipantsAsync(Stream fileStream)
        {
            try
            {
                using var document = await JsonDocument.ParseAsync(fileStream);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    throw new BusinessException(_localizer["StudentImportInvalidJson"]);
                }

                var participants = new List<Participant>();
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var child in element.EnumerateArray())
                        {
                            if (child.ValueKind == JsonValueKind.Object)
                            {
                                participants.Add(ReadParticipant(child));
                            }
                        }
                    }
                    else if (element.ValueKind == JsonValueKind.Object)
                    {
                        participants.Add(ReadParticipant(element));
                    }
                }

                return participants;
            }
            catch (JsonException)
            {
                throw new BusinessException(_localizer["StudentImportInvalidJson"]);
            }
        }

        private static Participant ReadParticipant(JsonElement element)
        {
            return new Participant
            {
                FirstName = GetJsonString(element, "nome"),
                LastName = GetJsonString(element, "sobrenome"),
                LastAccessAt = GetJsonString(element, "ltimoacesso"),
                Email = GetJsonString(element, "endereodee-mail")
            };
        }

        private static string? GetJsonString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }

            return null;
        }

        private async Task EnsureStudentUcLinkAsync(string? studentId, UcDocument? uc)
        {
            if (string.IsNullOrWhiteSpace(studentId) || uc?.Id == null)
            {
                return;
            }

            await _educationRepository.EnsureStudentUcMapAsync(studentId, uc.Id);
        }

        private sealed class Participant
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? LastAccessAt { get; set; }
            public string? Email { get; set; }
        }
    }
}
