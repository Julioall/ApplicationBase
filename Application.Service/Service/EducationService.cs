using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface.Education;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model.Students;
using Application.Service.Interface;
using Microsoft.Extensions.Localization;

namespace Application.Service.Service
{
    public class EducationService : IEducationService
    {
        private readonly IEducationRepository _educationRepository;
        private readonly IEducationImportRepository _educationImportRepository;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private static readonly Regex BreakRegex = new("<br\\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled);

        public EducationService(IEducationRepository educationRepository, IEducationImportRepository educationImportRepository, IStringLocalizer<SharedResource> localizer)
        {
            _educationRepository = educationRepository;
            _educationImportRepository = educationImportRepository;
            _localizer = localizer;
        }

        public async Task<EducationImport> EnqueueImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fileStream);

            if (fileStream.Length == 0)
            {
                throw new BusinessException(_localizer["CourseImportFileEmpty"]);
            }

            if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(_localizer["CourseImportOnlyJson"]);
            }

            var import = new EducationImport
            {
                FileName = fileName,
                Status = EducationImportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _educationImportRepository.AddAsync(import, fileStream, "application/json", cancellationToken);
            return import;
        }

        public async Task<CourseImportResult> ImportCoursesAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fileStream);

            if (fileStream.Length == 0)
            {
                throw new BusinessException(_localizer["CourseImportFileEmpty"]);
            }

            if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(_localizer["CourseImportOnlyJson"]);
            }

            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            JsonDocument document;
            try
            {
                document = await JsonDocument.ParseAsync(fileStream, cancellationToken: cancellationToken);
            }
            catch (Exception)
            {
                throw new BusinessException(_localizer["CourseImportInvalidJson"]);
            }

            using var _ = document;
            var coursesElement = ExtractCoursesElement(document.RootElement);
            var result = new CourseImportResult();
            var classPeriods = new Dictionary<string, (long Min, long Max, ClassDocument ClassDoc)>();

            foreach (var courseElement in coursesElement.EnumerateArray())
            {
                result.Processed++;
                try
                {
                    await ProcessCourseAsync(courseElement, classPeriods, result, cancellationToken);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new CourseImportError
                    {
                        CourseCategory = TryGetString(courseElement, "coursecategory"),
                        UcName = TryGetString(courseElement, "fullname"),
                        Message = ex.Message
                    });
                }
            }

            foreach (var period in classPeriods.Values)
            {
                if (period.ClassDoc.StartDate != period.Min || period.ClassDoc.EndDate != period.Max)
                {
                    period.ClassDoc.StartDate = period.Min;
                    period.ClassDoc.EndDate = period.Max;
                }
            }

            return result;
        }

        public Task<IReadOnlyCollection<School>> GetSchoolsAsync(CancellationToken cancellationToken = default)
        {
            return _educationRepository.GetSchoolsAsync(cancellationToken);
        }

        public Task<IReadOnlyCollection<ProgramDocument>> GetProgramsBySchoolAsync(string schoolId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(schoolId);
            return _educationRepository.GetProgramsBySchoolAsync(schoolId, cancellationToken);
        }

        public Task<IReadOnlyCollection<ClassDocument>> GetClassesByProgramAsync(string programId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(programId);
            return _educationRepository.GetClassesByProgramAsync(programId, cancellationToken);
        }

        public Task<IReadOnlyCollection<UcDocument>> GetUcsByClassAsync(string classId, string? search = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(classId);
            return _educationRepository.GetUcsByClassAsync(classId, search, cancellationToken);
        }

        public Task<PagedResult<UcDocument>> SearchUcsAsync(PaginationQuery query, string? classId = null, string? programId = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            return _educationRepository.SearchUcsAsync(query.Search, classId, programId, query, cancellationToken);
        }

        public async Task<IReadOnlyCollection<Student>> GetStudentsByUcEadIdAsync(int eadId, CancellationToken cancellationToken = default)
        {
            if (eadId <= 0)
            {
                throw new ArgumentException("Invalid UC id.", nameof(eadId));
            }

            var uc = await _educationRepository.GetUcByEadIdAsync(eadId, cancellationToken);
            if (uc == null || string.IsNullOrWhiteSpace(uc.Id))
            {
                return Array.Empty<Student>();
            }

            return await _educationRepository.GetStudentsByUcAsync(uc.Id, cancellationToken);
        }

        public Task<EducationImport?> GetImportAsync(string id, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return _educationImportRepository.GetByIdAsync(id, cancellationToken);
        }

        internal async Task ProcessCourseAsync(JsonElement courseElement, Dictionary<string, (long Min, long Max, ClassDocument ClassDoc)> classPeriods, CourseImportResult result, CancellationToken cancellationToken)
        {
            var eadId = GetRequiredInt(courseElement, "id");
            var fullname = GetRequiredString(courseElement, "fullname");
            var startDate = GetRequiredLong(courseElement, "startdate");
            var endDate = GetRequiredLong(courseElement, "enddate");
            var viewUrl = TryGetString(courseElement, "viewurl");
            var courseImage = TryGetString(courseElement, "courseimage");
            var courseCategory = GetRequiredString(courseElement, "coursecategory");
            var summary = TryGetString(courseElement, "summary") ?? string.Empty;

            var lines = ParseSummary(summary);
            var programName = lines.ElementAtOrDefault(0);
            var schoolName = lines.ElementAtOrDefault(1);
            var periodText = lines.ElementAtOrDefault(2);

            if (string.IsNullOrWhiteSpace(programName))
            {
                throw new BusinessException(_localizer["CourseImportProgramMissing"]);
            }

            if (string.IsNullOrWhiteSpace(schoolName))
            {
                throw new BusinessException(_localizer["CourseImportSchoolMissing"]);
            }

            var school = await _educationRepository.UpsertSchoolAsync(schoolName!, cancellationToken);
            var program = await _educationRepository.UpsertProgramAsync(school.Id!, programName!, cancellationToken);
            var classDoc = await _educationRepository.UpsertClassAsync(school.Id!, program.Id!, courseCategory, courseCategory, cancellationToken);

            if (!classPeriods.TryGetValue(classDoc.Id!, out var currentPeriod))
            {
                currentPeriod = (startDate, endDate, classDoc);
            }
            else
            {
                currentPeriod = (Math.Min(currentPeriod.Min, startDate), Math.Max(currentPeriod.Max, endDate), classDoc);
            }
            classPeriods[classDoc.Id!] = currentPeriod;

            var ucToUpsert = new UcDocument
            {
                EadId = eadId,
                Fullname = fullname,
                StartDate = startDate,
                EndDate = endDate,
                ViewUrl = viewUrl,
                CourseImage = courseImage,
                CourseCategory = courseCategory,
                SchoolNameDerived = schoolName,
                ProgramNameDerived = programName,
                PeriodTextDerived = periodText
            };

            var upsertResult = await _educationRepository.UpsertUcAsync(ucToUpsert, cancellationToken);
            await _educationRepository.EnsureClassUcMapAsync(classDoc.Id!, $"ucs/{eadId}", cancellationToken);

            if (upsertResult.Created)
            {
                result.CreatedUcs++;
            }
            else
            {
                result.UpdatedUcs++;
            }

            result.Linked++;
        }

        private List<string> ParseSummary(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                return new List<string>();
            }

            var decoded = WebUtility.HtmlDecode(summary);
            var withBreaks = BreakRegex.Replace(decoded, "\n");
            var withoutTags = HtmlTagRegex.Replace(withBreaks, "\n");

            return withoutTags
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }

        private JsonElement ExtractCoursesElement(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (item.TryGetProperty("data", out var dataFromArray) &&
                        dataFromArray.TryGetProperty("courses", out var coursesFromArray) &&
                        coursesFromArray.ValueKind == JsonValueKind.Array)
                    {
                        return coursesFromArray;
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Object &&
                     root.TryGetProperty("data", out var dataElement) &&
                     dataElement.TryGetProperty("courses", out var coursesElement) &&
                     coursesElement.ValueKind == JsonValueKind.Array)
            {
                return coursesElement;
            }

            throw new BusinessException(_localizer["CourseImportCoursesMissing"]);
        }

        private string GetRequiredString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            {
                throw new BusinessException(_localizer["CourseImportFieldMissing", propertyName]);
            }

            var text = value.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new BusinessException(_localizer["CourseImportFieldMissing", propertyName]);
            }

            return text!;
        }

        private int GetRequiredInt(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                throw new BusinessException(_localizer["CourseImportFieldMissing", propertyName]);
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
            {
                return intValue;
            }

            throw new BusinessException(_localizer["CourseImportFieldMissing", propertyName]);
        }

        private long GetRequiredLong(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                throw new BusinessException(_localizer["CourseImportFieldMissing", propertyName]);
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var longValue))
            {
                return longValue;
            }

            throw new BusinessException(_localizer["CourseImportFieldMissing", propertyName]);
        }

        private static string? TryGetString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            return null;
        }
    }
}
