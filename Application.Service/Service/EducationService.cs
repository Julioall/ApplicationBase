using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface;
using Application.Domain.Interface.Education;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model.Students;
using Application.Service.Interface;
using Application.Service.Education;
using Application.Service.Education.Parsers;
using Application.Shared.Background;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Application.Service.Service
{
    public class EducationService : IEducationService
    {
        private readonly IEducationRepository _educationRepository;
        private readonly IEducationImportRepository _educationImportRepository;
        private readonly IStudentUcPerformanceRepository _studentUcPerformanceRepository;
        private readonly IExcelReportParser _excelReportParser;
        private readonly IEducationReportImportProcessor _importProcessor;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ILogger<EducationService> _logger;
        private readonly IBackgroundJobScheduler _backgroundJobScheduler;
        private static readonly Regex BreakRegex = new("<br\\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled);

        public EducationService(
            IEducationRepository educationRepository, 
            IEducationImportRepository educationImportRepository,
            IStudentUcPerformanceRepository studentUcPerformanceRepository,
            IExcelReportParser excelReportParser,
            IEducationReportImportProcessor importProcessor,
            IStringLocalizer<SharedResource> localizer,
            ILogger<EducationService> logger,
            IBackgroundJobScheduler backgroundJobScheduler)
        {
            _educationRepository = educationRepository;
            _educationImportRepository = educationImportRepository;
            _studentUcPerformanceRepository = studentUcPerformanceRepository;
            _excelReportParser = excelReportParser;
            _importProcessor = importProcessor;
            _localizer = localizer;
            _logger = logger;
            _backgroundJobScheduler = backgroundJobScheduler;
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
            
            // Enfileirar o job Hangfire para processar a importação
            _backgroundJobScheduler.Enqueue<IEducationImportJob>(
                job => job.ProcessImportAsync(import.Id!, cancellationToken));
            
            _logger.LogInformation("Education import {ImportId} enqueued for processing", import.Id);
            
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

        public async Task<IReadOnlyCollection<StudentUcDto>> GetStudentsByUcEadIdWithPerformanceAsync(int eadId, CancellationToken cancellationToken = default)
        {
            if (eadId <= 0)
            {
                throw new ArgumentException("Invalid UC id.", nameof(eadId));
            }

            var uc = await _educationRepository.GetUcByEadIdAsync(eadId, cancellationToken);
            if (uc == null || string.IsNullOrWhiteSpace(uc.Id))
            {
                return Array.Empty<StudentUcDto>();
            }

            return await _educationRepository.GetStudentsByUcWithPerformanceAsync(uc.Id, cancellationToken);
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

        private static List<string> ParseSummary(string summary)
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

        public async Task<EducationReportImportResult> ImportReportAsync(IEnumerable<(string fileName, Stream fileStream)> files, CancellationToken cancellationToken = default)
        {
            var result = new EducationReportImportResult();
            var fileList = files.ToList();

            _logger.LogInformation("EducationService: Iniciando importação de relatório com {FileCount} arquivo(s)", fileList.Count);

            if (fileList.Count == 0)
            {
                _logger.LogWarning("EducationService: Nenhum arquivo fornecido para importação");
                throw new ArgumentException(_localizer["ReportImportFilesEmpty"]);
            }

            // Validar extensões
            foreach (var (fileName, _) in fileList)
            {
                if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("EducationService: Arquivo {FileName} com extensão inválida (não é .xlsx)", fileName);
                    throw new ArgumentException(_localizer["ReportImportOnlyXlsx"]);
                }
            }

            // Processar cada arquivo
            var allRows = new List<EducationReportRow>();

            foreach (var (fileName, fileStream) in fileList)
            {
                _logger.LogDebug("EducationService: Processando arquivo {FileName}", fileName);
                try
                {
                    var rows = await _excelReportParser.ParseAsync(fileStream);
                    _logger.LogDebug("EducationService: Arquivo {FileName} retornou {RowCount} linhas", fileName, rows.Count);
                    allRows.AddRange(rows);
                    result.FilesProcessed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EducationService: Erro ao processar arquivo {FileName}", fileName);
                    throw;
                }
            }

            result.RowsRead = allRows.Count;
            _logger.LogInformation("EducationService: Total de {RowCount} linhas lidas de {FileCount} arquivo(s)", 
                allRows.Count, fileList.Count);

            // Processar as linhas lidas
            if (allRows.Count > 0)
            {
                _logger.LogDebug("EducationService: Iniciando processamento de linhas...");
                result = await _importProcessor.ProcessRowsAsync(allRows);
                result.FilesProcessed = fileList.Count;
                
                _logger.LogInformation("EducationService: Processamento concluído. Estudantes criados: {Created}, Atualizados: {Updated}, Erros: {Errors}", 
                    result.StudentsCreated, result.StudentsUpdated, result.Errors.Count);
            }
            else
            {
                _logger.LogWarning("EducationService: Nenhuma linha foi lida dos arquivos");
            }

            return result;
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

        public async Task<IEnumerable<string>> GetUcsByStudentAsync(string studentId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(studentId);
            return await _studentUcPerformanceRepository.GetUcsByStudentAsync(studentId);
        }

        public async Task<StudentUcPerformance?> GetStudentUcPerformanceAsync(string studentId, string ucId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(studentId);
            ArgumentNullException.ThrowIfNull(ucId);
            return await _studentUcPerformanceRepository.GetByStudentAndUcAsync(studentId, ucId);
        }

        public async Task ToggleActivityHiddenAsync(string studentId, string ucId, string activityName, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(studentId);
            ArgumentNullException.ThrowIfNull(ucId);
            ArgumentNullException.ThrowIfNull(activityName);

            // Validar que a atividade existe
            var performance = await _studentUcPerformanceRepository.GetByStudentAndUcAsync(studentId, ucId);
            if (performance == null)
                throw new NotFoundException("Desempenho do estudante não encontrado");

            var activity = performance.Activities.FirstOrDefault(a => 
                a.Name.Equals(activityName, StringComparison.OrdinalIgnoreCase));
            if (activity == null)
                throw new NotFoundException("Atividade não encontrada");

            // Carregar ou criar config de atividades ocultas
            var config = await _studentUcPerformanceRepository.GetHiddenActivitiesAsync(ucId);
            if (config == null)
            {
                config = new HiddenActivitiesConfig { UcId = ucId };
            }

            // Toggle
            config.ToggleActivityName(activityName);
            await _studentUcPerformanceRepository.SaveHiddenActivitiesAsync(config);
        }
    }
}
