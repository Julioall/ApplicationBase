using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using Application.Domain.Interface.Education;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education;
using Application.Infrastructure.Indexes;
using Application.Infrastructure.Service;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model.Students;

namespace Application.Infrastructure.Repository.Education
{
    public class EducationRepository : IEducationRepository
    {
        private readonly IServiceRavenDB _serviceRavenDb;
        private const string SyncStatusId = "education/sync-status";
        private static readonly Regex NonAlphaNumeric = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public EducationRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task<School> UpsertSchoolAsync(string name, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var id = $"schools/{Slugify(name)}";
            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<School>(id, cancellationToken);
            if (existing != null)
            {
                if (!string.Equals(existing.Name, name, StringComparison.Ordinal))
                {
                    existing.Name = name;
                    await _serviceRavenDb.AsyncSession.StoreAsync(existing, id, cancellationToken);
                }
                return existing;
            }

            var school = new School
            {
                Id = id,
                Name = name
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(school, id, cancellationToken);
            return school;
        }

        public async Task<ProgramDocument> UpsertProgramAsync(string schoolId, string name, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(schoolId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var schoolSlug = ExtractSlug(schoolId, "schools/");
            var id = $"programs/{schoolSlug}-{Slugify(name)}";

            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<ProgramDocument>(id, cancellationToken);
            if (existing != null)
            {
                if (!string.Equals(existing.Name, name, StringComparison.Ordinal) || !string.Equals(existing.SchoolId, schoolId, StringComparison.Ordinal))
                {
                    existing.Name = name;
                    existing.SchoolId = schoolId;
                    await _serviceRavenDb.AsyncSession.StoreAsync(existing, id, cancellationToken);
                }

                return existing;
            }

            var program = new ProgramDocument
            {
                Id = id,
                SchoolId = schoolId,
                Name = name
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(program, id, cancellationToken);
            return program;
        }

        public async Task<ClassDocument> UpsertClassAsync(string schoolId, string programId, string courseCategoryRaw, string name, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(schoolId);
            ArgumentException.ThrowIfNullOrWhiteSpace(programId);
            ArgumentException.ThrowIfNullOrWhiteSpace(courseCategoryRaw);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var id = $"classes/{Slugify(courseCategoryRaw)}";
            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<ClassDocument>(id, cancellationToken);
            if (existing != null)
            {
                var hasChanges = false;
                if (!string.Equals(existing.Name, name, StringComparison.Ordinal))
                {
                    existing.Name = name;
                    hasChanges = true;
                }

                if (!string.Equals(existing.CourseCategoryRaw, courseCategoryRaw, StringComparison.Ordinal))
                {
                    existing.CourseCategoryRaw = courseCategoryRaw;
                    hasChanges = true;
                }

                if (!string.Equals(existing.SchoolId, schoolId, StringComparison.Ordinal))
                {
                    existing.SchoolId = schoolId;
                    hasChanges = true;
                }

                if (!string.Equals(existing.ProgramId, programId, StringComparison.Ordinal))
                {
                    existing.ProgramId = programId;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    await _serviceRavenDb.AsyncSession.StoreAsync(existing, id, cancellationToken);
                }

                return existing;
            }

            var classDoc = new ClassDocument
            {
                Id = id,
                SchoolId = schoolId,
                ProgramId = programId,
                Name = name,
                CourseCategoryRaw = courseCategoryRaw
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(classDoc, id, cancellationToken);
            return classDoc;
        }

        public async Task<UcUpsertResult> UpsertUcAsync(UcDocument uc, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(uc);
            var id = $"ucs/{uc.EadId}";

            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<UcDocument>(id, cancellationToken);
            if (existing == null)
            {
                uc.Id = id;
                await _serviceRavenDb.AsyncSession.StoreAsync(uc, id, cancellationToken);
                return new UcUpsertResult
                {
                    Created = true,
                    Updated = true,
                    Entity = uc
                };
            }

            var updated = false;
            if (!string.Equals(existing.Fullname, uc.Fullname, StringComparison.Ordinal))
            {
                existing.Fullname = uc.Fullname;
                updated = true;
            }

            if (existing.StartDate != uc.StartDate)
            {
                existing.StartDate = uc.StartDate;
                updated = true;
            }

            if (existing.EndDate != uc.EndDate)
            {
                existing.EndDate = uc.EndDate;
                updated = true;
            }

            if (!string.Equals(existing.ViewUrl, uc.ViewUrl, StringComparison.Ordinal))
            {
                existing.ViewUrl = uc.ViewUrl;
                updated = true;
            }

            if (!string.Equals(existing.CourseImage, uc.CourseImage, StringComparison.Ordinal))
            {
                existing.CourseImage = uc.CourseImage;
                updated = true;
            }

            if (!string.Equals(existing.CourseCategory, uc.CourseCategory, StringComparison.Ordinal))
            {
                existing.CourseCategory = uc.CourseCategory;
                updated = true;
            }

            if (!string.Equals(existing.SchoolNameDerived, uc.SchoolNameDerived, StringComparison.Ordinal))
            {
                existing.SchoolNameDerived = uc.SchoolNameDerived;
                updated = true;
            }

            if (!string.Equals(existing.ProgramNameDerived, uc.ProgramNameDerived, StringComparison.Ordinal))
            {
                existing.ProgramNameDerived = uc.ProgramNameDerived;
                updated = true;
            }

            if (!string.Equals(existing.PeriodTextDerived, uc.PeriodTextDerived, StringComparison.Ordinal))
            {
                existing.PeriodTextDerived = uc.PeriodTextDerived;
                updated = true;
            }

            if (updated)
            {
                await _serviceRavenDb.AsyncSession.StoreAsync(existing, id, cancellationToken);
            }

            return new UcUpsertResult
            {
                Created = false,
                Updated = updated,
                Entity = existing
            };
        }

        public Task<UcDocument?> GetUcByEadIdAsync(int eadId, CancellationToken cancellationToken = default)
        {
            var id = $"ucs/{eadId}";
            return _serviceRavenDb.AsyncSession.LoadAsync<UcDocument?>(id, cancellationToken);
        }

        public async Task<ClassUcMap> EnsureClassUcMapAsync(string classId, string ucId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(classId);
            ArgumentException.ThrowIfNullOrWhiteSpace(ucId);

            var mapId = $"class_uc_maps/{classId}/{ucId}";
            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<ClassUcMap>(mapId, cancellationToken);
            if (existing != null)
            {
                return existing;
            }

            var map = new ClassUcMap
            {
                Id = mapId,
                ClassId = classId,
                UcId = ucId,
                ImportedAt = DateTime.UtcNow
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(map, mapId, cancellationToken);
            return map;
        }

        public async Task<StudentUcMap> EnsureStudentUcMapAsync(string studentId, string ucId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(studentId);
            ArgumentException.ThrowIfNullOrWhiteSpace(ucId);

            var mapId = $"student_uc_maps/{studentId}/{ucId}";
            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<StudentUcMap>(mapId, cancellationToken);
            if (existing != null)
            {
                return existing;
            }

            var map = new StudentUcMap
            {
                Id = mapId,
                StudentId = studentId,
                UcId = ucId,
                ImportedAt = DateTime.UtcNow
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(map, mapId, cancellationToken);
            return map;
        }

        public async Task<IReadOnlyCollection<Student>> GetStudentsByUcAsync(string ucId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ucId);

            var mappings = await _serviceRavenDb.AsyncSession.Query<StudentUcMap, StudentUcMaps_ByUc>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(m => m.UcId == ucId)
                .ToListAsync(cancellationToken);

            if (mappings.Count == 0)
            {
                return Array.Empty<Student>();
            }

            var studentIds = mappings.Select(m => m.StudentId).Distinct().ToList();
            var loaded = await _serviceRavenDb.AsyncSession.LoadAsync<Student>(studentIds, cancellationToken);

            return loaded.Values
                .Where(s => s != null)
                .Select(s => s!)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();
        }

        public async Task<IReadOnlyCollection<StudentUcDto>> GetStudentsByUcWithPerformanceAsync(string ucId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ucId);

            // Buscar mapeamentos de studentId -> ucId
            var mappings = await _serviceRavenDb.AsyncSession.Query<StudentUcMap, StudentUcMaps_ByUc>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(m => m.UcId == ucId)
                .ToListAsync(cancellationToken);

            if (mappings.Count == 0)
            {
                return Array.Empty<StudentUcDto>();
            }

            var studentIds = mappings.Select(m => m.StudentId).Distinct().ToList();

            // Carregar estudantes
            var students = await _serviceRavenDb.AsyncSession.LoadAsync<Student>(studentIds, cancellationToken);

            // Buscar performances para este UC (sem usar variável capturada)
            // RavenDB não consegue resolver variáveis locais em Where, então convertemos ucId para constante dentro da query
            var performancesList = await _serviceRavenDb.AsyncSession.Query<StudentUcPerformance>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(p => p.UcId == ucId)
                .ToListAsync(cancellationToken);

            // Filtrar performances apenas para os students desta UC
            var performances = performancesList
                .Where(p => studentIds.Contains(p.StudentId))
                .GroupBy(p => p.StudentId)
                .Select(g => g.First()) // Pegar apenas o primeiro (não deveria haver duplicatas)
                .ToList();

            // Criar dicionário de performance por studentId para lookup rápido
            var performanceDict = performances
                .ToDictionary(p => p.StudentId, p => p);

            // Carregar configuração de atividades ocultas para esta UC
            var hiddenConfig = await _serviceRavenDb.AsyncSession
                .Query<HiddenActivitiesConfig>()
                .Where(c => c.UcId == ucId)
                .FirstOrDefaultAsync(cancellationToken);

            var hiddenActivityNames = hiddenConfig?.HiddenActivityNames ?? new List<string>();
            var hiddenActivitySet = hiddenActivityNames
                .Select(NormalizeActivityName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Combinar dados em DTO
            var result = new List<StudentUcDto>();
            foreach (var student in students.Values)
            {
                if (student == null) continue;

                var dto = new StudentUcDto
                {
                    Id = student.Id,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Email = student.Email,
                    IdNumber = student.IdNumber,
                    Phone = student.Phone,
                    Institution = student.Institution,
                    IsActive = student.IsActive,
                    Status = student.Status,
                    LastAccessAt = student.LastAccessAt,
                    CreatedAt = student.CreatedAt,
                    UpdatedAt = student.UpdatedAt
                };

                // Adicionar performance se existir
                if (performanceDict.TryGetValue(student.Id, out var performance))
                {
                    dto.Activities = performance.Activities
                        .Select(a =>
                        {
                            var normalizedName = NormalizeActivityName(a.Name);
                            var isHidden = hiddenActivitySet.Contains(normalizedName);

                            return new StudentActivityDto
                            {
                                Name = a.Name,
                                FinalGrade = a.FinalGrade,
                                SubmittedAt = a.SubmittedAt,
                                CorrectedAt = a.CorrectedAt,
                                SubmissionStatus = a.SubmissionStatus,
                                Restriction = a.Restriction,
                                StartAt = a.StartAt,
                                EndAt = a.EndAt,
                                Type = (int)a.Type,
                                CorrectionStatus = a.GetCorrectionStatus(),
                                IsPendingCorrection = a.IsPendingCorrection(),
                                HasRestriction = a.HasRestriction(),
                                IsLate = a.IsLate(),
                                Hidden = isHidden
                            };
                        })
                        .ToList();

                    var visibleActivityGrades = dto.Activities
                        .Where(a => !a.Hidden && a.FinalGrade.HasValue)
                        .Select(a => a.FinalGrade!.Value)
                        .ToList();

                    dto.FinalGrade = visibleActivityGrades.Count > 0
                        ? visibleActivityGrades.Sum()
                        : null;
                }

                result.Add(dto);
            }

            return result
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();
        }

        public async Task<IReadOnlyCollection<School>> GetSchoolsAsync(CancellationToken cancellationToken = default)
        {
            var schools = await _serviceRavenDb.AsyncSession.Query<School>()
                .Customize(x => x.WaitForNonStaleResults())
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            return schools;
        }

        public async Task<IReadOnlyCollection<ProgramDocument>> GetProgramsBySchoolAsync(string schoolId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(schoolId);

            var programs = await _serviceRavenDb.AsyncSession.Query<ProgramDocument>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(p => p.SchoolId == schoolId)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            return programs;
        }

        public async Task<IReadOnlyCollection<ClassDocument>> GetClassesByProgramAsync(string programId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(programId);

            var classes = await _serviceRavenDb.AsyncSession.Query<ClassDocument, Classes_BySchoolAndProgram>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(c => c.ProgramId == programId)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return classes;
        }

        public async Task<IReadOnlyCollection<UcDocument>> GetUcsByClassAsync(string classId, string? search = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(classId);

            search = NormalizeNull(search);

            var query = _serviceRavenDb.AsyncSession.Query<ClassUcMap, UcSearchIndex>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(m => m.ClassId == classId)
                .ProjectInto<UcSearchIndex.Result>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Search(x => x.Fullname, search);
            }

            var results = await query
                .OrderBy(x => x.StartDate)
                .ThenBy(x => x.Fullname)
                .ToListAsync(cancellationToken);

            if (results.Count == 0)
            {
                return Array.Empty<UcDocument>();
            }

            var ucIds = results.Select(m => m.UcId).Distinct().ToList();
            var loaded = await _serviceRavenDb.AsyncSession.LoadAsync<UcDocument>(ucIds, cancellationToken);

            return results
                .Select(r => loaded.TryGetValue(r.UcId, out var uc) ? uc : null)
                .Where(uc => uc != null)
                .Select(uc => uc!)
                .ToList();
        }

        public async Task<PagedResult<UcDocument>> SearchUcsAsync(string? search, string? classId, string? programId, PaginationQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            search = NormalizeNull(query.Search ?? search);
            classId = NormalizeNull(classId);
            programId = NormalizeNull(programId);

            var ravenQuery = _serviceRavenDb.AsyncSession.Query<ClassUcMap, UcSearchIndex>()
                .Customize(x => x.WaitForNonStaleResults())
                .ProjectInto<UcSearchIndex.Result>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                ravenQuery = ravenQuery.Search(x => x.Fullname, search);
            }

            if (!string.IsNullOrWhiteSpace(classId))
            {
                ravenQuery = ravenQuery.Where(x => x.ClassId == classId);
            }

            if (!string.IsNullOrWhiteSpace(programId))
            {
                ravenQuery = ravenQuery.Where(x => x.ProgramId == programId);
            }

            var total = await ravenQuery.CountAsync(cancellationToken);
            var results = await ravenQuery
                .OrderByDescending(x => x.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            if (results.Count == 0)
            {
                return new PagedResult<UcDocument>
                {
                    Items = Array.Empty<UcDocument>(),
                    Total = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            var ucIds = results.Select(r => r.UcId).Distinct().ToList();
            var loaded = await _serviceRavenDb.AsyncSession.LoadAsync<UcDocument>(ucIds, cancellationToken);
            var items = results
                .Select(r => loaded.TryGetValue(r.UcId, out var uc) ? uc : null)
                .Where(uc => uc != null)
                .Select(uc => uc!)
                .ToList();

            return new PagedResult<UcDocument>
            {
                Items = items,
                Total = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task RecalculateClassPeriodAsync(string classId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(classId);

            var classDoc = await _serviceRavenDb.AsyncSession.LoadAsync<ClassDocument>(classId, cancellationToken);
            if (classDoc == null)
            {
                return;
            }

            var mappings = await _serviceRavenDb.AsyncSession.Query<ClassUcMap, ClassUcMaps_ByClass>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(m => m.ClassId == classId)
                .ToListAsync(cancellationToken);

            if (mappings.Count == 0)
            {
                classDoc.StartDate = null;
                classDoc.EndDate = null;
                await _serviceRavenDb.AsyncSession.StoreAsync(classDoc, classId, cancellationToken);
                return;
            }

            var ucIds = mappings.Select(m => m.UcId).Distinct().ToList();
            var loaded = await _serviceRavenDb.AsyncSession.LoadAsync<UcDocument>(ucIds, cancellationToken);
            var ucs = loaded.Values.Where(v => v != null).Select(v => v!).ToList();

            if (ucs.Count == 0)
            {
                classDoc.StartDate = null;
                classDoc.EndDate = null;
                await _serviceRavenDb.AsyncSession.StoreAsync(classDoc, classId, cancellationToken);
                return;
            }

            var start = ucs.Min(u => u.StartDate);
            var end = ucs.Max(u => u.EndDate);

            if (classDoc.StartDate != start || classDoc.EndDate != end)
            {
                classDoc.StartDate = start;
                classDoc.EndDate = end;
                await _serviceRavenDb.AsyncSession.StoreAsync(classDoc, classId, cancellationToken);
            }
        }

        public async Task<EducationSyncStatus> GetSyncStatusAsync(CancellationToken cancellationToken = default)
        {
            var status = await _serviceRavenDb.AsyncSession.LoadAsync<EducationSyncStatus>(SyncStatusId, cancellationToken);
            if (status != null)
            {
                return status;
            }

            status = new EducationSyncStatus
            {
                Id = SyncStatusId,
                Status = "NotStarted",
                LastSyncAt = null,
                ExpiresAt = null,
                Message = null,
                TriggeredAt = null,
                TriggeredByName = null,
                TriggeredByUserId = null
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(status, SyncStatusId, cancellationToken);
            return status;
        }

        public async Task UpdateSyncStatusAsync(EducationSyncStatus status, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(status);

            var id = string.IsNullOrWhiteSpace(status.Id) ? SyncStatusId : status.Id;
            status.Id = id;
            await _serviceRavenDb.AsyncSession.StoreAsync(status, id, cancellationToken);
        }

        private static string NormalizeNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string NormalizeActivityName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return Regex.Replace(name.Trim(), "\\s+", " ", RegexOptions.Compiled)
                .ToLowerInvariant();
        }

        private static string ExtractSlug(string id, string prefix)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(prefix))
            {
                return string.Empty;
            }

            return id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? id.Substring(prefix.Length)
                : id;
        }

        private static string Slugify(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            var cleaned = NonAlphaNumeric.Replace(builder.ToString().ToLowerInvariant(), "-");
            cleaned = cleaned.Trim('-');
            return string.IsNullOrWhiteSpace(cleaned) ? "item" : cleaned;
        }
    }
}
