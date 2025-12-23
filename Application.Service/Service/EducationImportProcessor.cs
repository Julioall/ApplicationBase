using System.Net;
using System.Text.RegularExpressions;
using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface.Education;
using Application.Domain.Model.Education;
using Microsoft.Extensions.Localization;
using System.Text.Json;
using Application.Domain.Model.Education.Dtos;

namespace Application.Service.Service
{
    public class EducationImportProcessor
    {
        private readonly IEducationRepository _educationRepository;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private static readonly Regex BreakRegex = new("<br\\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled);

        public EducationImportProcessor(IEducationRepository educationRepository, IStringLocalizer<SharedResource> localizer)
        {
            _educationRepository = educationRepository;
            _localizer = localizer;
        }

        public async Task ProcessBatchAsync(IEnumerable<JsonElement> courses, Dictionary<string, (long Min, long Max, ClassDocument ClassDoc)> classPeriods, CourseImportResult result, CancellationToken cancellationToken)
        {
            foreach (var courseElement in courses)
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
        }

        private async Task ProcessCourseAsync(JsonElement courseElement, Dictionary<string, (long Min, long Max, ClassDocument ClassDoc)> classPeriods, CourseImportResult result, CancellationToken cancellationToken)
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
            else if (upsertResult.Updated)
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
