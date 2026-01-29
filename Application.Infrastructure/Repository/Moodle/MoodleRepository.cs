using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using Application.Domain.Interface.Moodle;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model.Moodle;
using Application.Domain.Model.Moodle.Dtos;
using Application.Domain.Model.Students;
using Application.Infrastructure.Indexes;
using Application.Infrastructure.Service;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace Application.Infrastructure.Repository.Moodle
{
    public class MoodleRepository : IMoodleRepository
    {
        private readonly IServiceRavenDB _serviceRavenDb;
        private const string SyncStatusId = "moodle/sync-status";
        private static readonly Regex NonAlphaNumeric = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public MoodleRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task<MoodleCategory> UpsertCategoryAsync(string name, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var id = $"moodle-categories/{Slugify(name)}";
            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCategory>(id, cancellationToken);
            if (existing != null)
            {
                if (!string.Equals(existing.Name, name, StringComparison.Ordinal))
                {
                    existing.Name = name;
                    await _serviceRavenDb.AsyncSession.StoreAsync(existing, id, cancellationToken);
                }
                return existing;
            }

            var category = new MoodleCategory
            {
                Id = id,
                Name = name
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(category, id, cancellationToken);
            return category;
        }

        public async Task<MoodleCourseCategory> UpsertCourseCategoryAsync(string categoryId, string name, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var categorySlug = ExtractSlug(categoryId, "moodle-categories/");
            var id = $"moodle-course-categories/{categorySlug}-{Slugify(name)}";

            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCourseCategory>(id, cancellationToken);
            if (existing != null)
            {
                if (!string.Equals(existing.Name, name, StringComparison.Ordinal) || !string.Equals(existing.CategoryId, categoryId, StringComparison.Ordinal))
                {
                    existing.Name = name;
                    existing.CategoryId = categoryId;
                    await _serviceRavenDb.AsyncSession.StoreAsync(existing, id, cancellationToken);
                }

                return existing;
            }

            var courseCategory = new MoodleCourseCategory
            {
                Id = id,
                CategoryId = categoryId,
                Name = name
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(courseCategory, id, cancellationToken);
            return courseCategory;
        }

        public async Task<MoodleCohort> UpsertCohortAsync(string categoryId, string courseCategoryId, string courseCategoryRaw, string name, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
            ArgumentException.ThrowIfNullOrWhiteSpace(courseCategoryId);
            ArgumentException.ThrowIfNullOrWhiteSpace(courseCategoryRaw);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var id = $"moodle-cohorts/{Slugify(courseCategoryRaw)}";
            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCohort>(id, cancellationToken);
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

                if (!string.Equals(existing.CategoryId, categoryId, StringComparison.Ordinal))
                {
                    existing.CategoryId = categoryId;
                    hasChanges = true;
                }

                if (!string.Equals(existing.CourseCategoryId, courseCategoryId, StringComparison.Ordinal))
                {
                    existing.CourseCategoryId = courseCategoryId;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    await _serviceRavenDb.AsyncSession.StoreAsync(existing, id, cancellationToken);
                }

                return existing;
            }

            var cohort = new MoodleCohort
            {
                Id = id,
                CategoryId = categoryId,
                CourseCategoryId = courseCategoryId,
                Name = name,
                CourseCategoryRaw = courseCategoryRaw
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(cohort, id, cancellationToken);
            return cohort;
        }

        public async Task<CourseUpsertResult> UpsertCourseAsync(MoodleCourse course, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(course);
            var id = $"moodle-courses/{course.EadId}";

            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCourse>(id, cancellationToken);
            if (existing == null)
            {
                course.Id = id;
                await _serviceRavenDb.AsyncSession.StoreAsync(course, id, cancellationToken);
                return new CourseUpsertResult
                {
                    Created = true,
                    Updated = true,
                    Entity = course
                };
            }

            var updated = false;
            if (!string.Equals(existing.Fullname, course.Fullname, StringComparison.Ordinal))
            {
                existing.Fullname = course.Fullname;
                updated = true;
            }

            if (existing.StartDate != course.StartDate)
            {
                existing.StartDate = course.StartDate;
                updated = true;
            }

            if (existing.EndDate != course.EndDate)
            {
                existing.EndDate = course.EndDate;
                updated = true;
            }

            if (!string.Equals(existing.ViewUrl, course.ViewUrl, StringComparison.Ordinal))
            {
                existing.ViewUrl = course.ViewUrl;
                updated = true;
            }

            if (!string.Equals(existing.CourseImage, course.CourseImage, StringComparison.Ordinal))
            {
                existing.CourseImage = course.CourseImage;
                updated = true;
            }

            if (!string.Equals(existing.CourseCategory, course.CourseCategory, StringComparison.Ordinal))
            {
                existing.CourseCategory = course.CourseCategory;
                updated = true;
            }

            if (!string.Equals(existing.CategoryNameDerived, course.CategoryNameDerived, StringComparison.Ordinal))
            {
                existing.CategoryNameDerived = course.CategoryNameDerived;
                updated = true;
            }

            if (!string.Equals(existing.CourseCategoryNameDerived, course.CourseCategoryNameDerived, StringComparison.Ordinal))
            {
                existing.CourseCategoryNameDerived = course.CourseCategoryNameDerived;
                updated = true;
            }

            if (!string.Equals(existing.PeriodTextDerived, course.PeriodTextDerived, StringComparison.Ordinal))
            {
                existing.PeriodTextDerived = course.PeriodTextDerived;
                updated = true;
            }

            if (updated)
            {
                await _serviceRavenDb.AsyncSession.StoreAsync(existing, id, cancellationToken);
            }

            return new CourseUpsertResult
            {
                Created = false,
                Updated = updated,
                Entity = existing
            };
        }

        public Task<MoodleCourse?> GetCourseByEadIdAsync(int eadId, CancellationToken cancellationToken = default)
        {
            var id = $"moodle-courses/{eadId}";
            return _serviceRavenDb.AsyncSession.LoadAsync<MoodleCourse?>(id, cancellationToken);
        }

        public async Task<MoodleCohortCourseMap> EnsureCohortCourseMapAsync(string cohortId, string courseId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cohortId);
            ArgumentException.ThrowIfNullOrWhiteSpace(courseId);

            var mapId = $"moodle-cohort-course-maps/{cohortId}/{courseId}";
            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCohortCourseMap>(mapId, cancellationToken);
            if (existing != null)
            {
                return existing;
            }

            var map = new MoodleCohortCourseMap
            {
                Id = mapId,
                CohortId = cohortId,
                CourseId = courseId
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(map, mapId, cancellationToken);
            return map;
        }

        public async Task<StudentCourseMap> EnsureStudentCourseMapAsync(string studentId, string courseId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(studentId);
            ArgumentException.ThrowIfNullOrWhiteSpace(courseId);

            var mapId = $"student-course-maps/{studentId}/{courseId}";
            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<StudentCourseMap>(mapId, cancellationToken);
            if (existing != null)
            {
                return existing;
            }

            var map = new StudentCourseMap
            {
                Id = mapId,
                StudentId = studentId,
                CourseId = courseId
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(map, mapId, cancellationToken);
            return map;
        }

        public async Task<IReadOnlyCollection<Student>> GetStudentsByCourseAsync(string courseId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(courseId);

            var mappings = await _serviceRavenDb.AsyncSession.Query<StudentCourseMap, StudentCourseMaps_ByCourse>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(m => m.CourseId == courseId)
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

        public async Task<IReadOnlyCollection<StudentCourseDto>> GetStudentsByCourseWithPerformanceAsync(string courseId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(courseId);

            var mappings = await _serviceRavenDb.AsyncSession.Query<StudentCourseMap, StudentCourseMaps_ByCourse>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(m => m.CourseId == courseId)
                .ToListAsync(cancellationToken);

            if (mappings.Count == 0)
            {
                return Array.Empty<StudentCourseDto>();
            }

            var studentIds = mappings.Select(m => m.StudentId).Distinct().ToList();
            var students = await _serviceRavenDb.AsyncSession.LoadAsync<Student>(studentIds, cancellationToken);

            var performancesList = await _serviceRavenDb.AsyncSession.Query<StudentCoursePerformance>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(p => p.CourseId == courseId)
                .ToListAsync(cancellationToken);

            var performances = performancesList
                .Where(p => studentIds.Contains(p.StudentId))
                .GroupBy(p => p.StudentId)
                .Select(g => g.First())
                .ToList();

            var performanceDict = performances.ToDictionary(p => p.StudentId, p => p);

            var hiddenConfig = await _serviceRavenDb.AsyncSession
                .Query<HiddenActivitiesConfig>()
                .Where(c => c.CourseId == courseId)
                .FirstOrDefaultAsync(cancellationToken);

            var hiddenActivityNames = hiddenConfig?.HiddenActivityNames ?? new List<string>();
            var hiddenActivitySet = hiddenActivityNames
                .Select(NormalizeActivityName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var result = new List<StudentCourseDto>();
            foreach (var student in students.Values)
            {
                if (student == null) continue;

                var dto = new StudentCourseDto
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

                if (performanceDict.TryGetValue(student.Id, out var performance))
                {
                    dto.Activities = performance.Activities
                        .Select(a =>
                        {
                            var normalizedName = NormalizeActivityName(a.Name);
                            var isHidden = hiddenActivitySet.Contains(normalizedName);

                            return new Domain.Model.Moodle.Dtos.StudentActivityDto
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

        public async Task<IReadOnlyCollection<MoodleCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var categories = await _serviceRavenDb.AsyncSession.Query<MoodleCategory>()
                .Customize(x => x.WaitForNonStaleResults())
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            return categories;
        }

        public async Task<IReadOnlyCollection<MoodleCourseCategory>> GetCourseCategoriesByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);

            var courseCategories = await _serviceRavenDb.AsyncSession.Query<MoodleCourseCategory>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(p => p.CategoryId == categoryId)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            return courseCategories;
        }

        public async Task<IReadOnlyCollection<MoodleCohort>> GetCohortsByCourseCategoryAsync(string courseCategoryId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(courseCategoryId);

            var cohorts = await _serviceRavenDb.AsyncSession.Query<MoodleCohort, MoodleCohorts_ByCategoryAndCourseCategory>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(c => c.CourseCategoryId == courseCategoryId)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return cohorts;
        }

        public async Task<IReadOnlyCollection<MoodleCourse>> GetCoursesByCohortAsync(string cohortId, string? search = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cohortId);

            search = NormalizeNull(search);

            var query = _serviceRavenDb.AsyncSession.Query<MoodleCohortCourseMap, MoodleCourseSearchIndex>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(m => m.CohortId == cohortId)
                .ProjectInto<MoodleCourseSearchIndex.Result>();

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
                return Array.Empty<MoodleCourse>();
            }

            var courseIds = results.Select(m => m.CourseId).Distinct().ToList();
            var loaded = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCourse>(courseIds, cancellationToken);

            return results
                .Select(r => loaded.TryGetValue(r.CourseId, out var course) ? course : null)
                .Where(course => course != null)
                .Select(course => course!)
                .ToList();
        }

        public async Task<PagedResult<MoodleCourse>> SearchCoursesAsync(string? search, string? cohortId, string? courseCategoryId, PaginationQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            search = NormalizeNull(query.Search ?? search);
            cohortId = NormalizeNull(cohortId);
            courseCategoryId = NormalizeNull(courseCategoryId);

            var ravenQuery = _serviceRavenDb.AsyncSession.Query<MoodleCohortCourseMap, MoodleCourseSearchIndex>()
                .Customize(x => x.WaitForNonStaleResults())
                .ProjectInto<MoodleCourseSearchIndex.Result>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                ravenQuery = ravenQuery.Search(x => x.Fullname, search);
            }

            if (!string.IsNullOrWhiteSpace(cohortId))
            {
                ravenQuery = ravenQuery.Where(x => x.CohortId == cohortId);
            }

            if (!string.IsNullOrWhiteSpace(courseCategoryId))
            {
                ravenQuery = ravenQuery.Where(x => x.CourseCategoryId == courseCategoryId);
            }

            var total = await ravenQuery.CountAsync(cancellationToken);
            var results = await ravenQuery
                .OrderByDescending(x => x.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            if (results.Count == 0)
            {
                return new PagedResult<MoodleCourse>
                {
                    Items = Array.Empty<MoodleCourse>(),
                    Total = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            var courseIds = results.Select(r => r.CourseId).Distinct().ToList();
            var loaded = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCourse>(courseIds, cancellationToken);
            var items = results
                .Select(r => loaded.TryGetValue(r.CourseId, out var course) ? course : null)
                .Where(course => course != null)
                .Select(course => course!)
                .ToList();

            return new PagedResult<MoodleCourse>
            {
                Items = items,
                Total = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task RecalculateCohortPeriodAsync(string cohortId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cohortId);

            var cohort = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCohort>(cohortId, cancellationToken);
            if (cohort == null)
            {
                return;
            }

            var mappings = await _serviceRavenDb.AsyncSession.Query<MoodleCohortCourseMap, MoodleCohortCourseMaps_ByCohort>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(m => m.CohortId == cohortId)
                .ToListAsync(cancellationToken);

            if (mappings.Count == 0)
            {
                cohort.StartDate = null;
                cohort.EndDate = null;
                await _serviceRavenDb.AsyncSession.StoreAsync(cohort, cohortId, cancellationToken);
                return;
            }

            var courseIds = mappings.Select(m => m.CourseId).Distinct().ToList();
            var loaded = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCourse>(courseIds, cancellationToken);
            var courses = loaded.Values.Where(v => v != null).Select(v => v!).ToList();

            if (courses.Count == 0)
            {
                cohort.StartDate = null;
                cohort.EndDate = null;
                await _serviceRavenDb.AsyncSession.StoreAsync(cohort, cohortId, cancellationToken);
                return;
            }

            var start = courses.Min(u => u.StartDate);
            var end = courses.Max(u => u.EndDate);

            if (cohort.StartDate != start || cohort.EndDate != end)
            {
                cohort.StartDate = start;
                cohort.EndDate = end;
                await _serviceRavenDb.AsyncSession.StoreAsync(cohort, cohortId, cancellationToken);
            }
        }

        public async Task<MoodleSyncStatus> GetSyncStatusAsync(CancellationToken cancellationToken = default)
        {
            var status = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleSyncStatus>(SyncStatusId, cancellationToken);
            if (status != null)
            {
                return status;
            }

            status = new MoodleSyncStatus
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

        public async Task UpdateSyncStatusAsync(MoodleSyncStatus status, CancellationToken cancellationToken = default)
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

        #region Category Management by MoodleId

        public async Task<MoodleCategory> UpsertCategoryByMoodleIdAsync(int moodleId, string name, int parentId, int depth, string? path, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var id = $"moodle-categories/{moodleId}";
            var existing = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCategory>(id, cancellationToken);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (existing != null)
            {
                var hasChanges = false;

                if (!string.Equals(existing.Name, name, StringComparison.Ordinal))
                {
                    existing.Name = name;
                    hasChanges = true;
                }
                if (existing.ParentId != parentId)
                {
                    existing.ParentId = parentId;
                    hasChanges = true;
                }
                if (existing.Depth != depth)
                {
                    existing.Depth = depth;
                    hasChanges = true;
                }
                if (!string.Equals(existing.Path, path, StringComparison.Ordinal))
                {
                    existing.Path = path;
                    hasChanges = true;
                }

                existing.LastSyncedAt = now;

                if (hasChanges)
                {
                    await _serviceRavenDb.AsyncSession.StoreAsync(existing, id, cancellationToken);
                }

                return existing;
            }

            var category = new MoodleCategory
            {
                Id = id,
                MoodleId = moodleId,
                Name = name,
                ParentId = parentId,
                Depth = depth,
                Path = path,
                LastSyncedAt = now
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(category, id, cancellationToken);
            return category;
        }

        public async Task<MoodleCategory?> GetCategoryByMoodleIdAsync(int moodleId, CancellationToken cancellationToken = default)
        {
            var id = $"moodle-categories/{moodleId}";
            return await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCategory>(id, cancellationToken);
        }

        public async Task<IReadOnlyDictionary<int, MoodleCategory>> GetCategoriesByMoodleIdsAsync(IEnumerable<int> moodleIds, CancellationToken cancellationToken = default)
        {
            var ids = moodleIds.Distinct().Select(id => $"moodle-categories/{id}").ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<int, MoodleCategory>();
            }

            var loaded = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCategory>(ids, cancellationToken);

            return loaded
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Value.MoodleId, kvp => kvp.Value);
        }

        public async Task<IReadOnlyDictionary<int, MoodleCategory>> UpsertCategoriesBatchAsync(IEnumerable<MoodleCategoryDto> categories, CancellationToken cancellationToken = default)
        {
            var categoryList = categories.ToList();
            if (categoryList.Count == 0)
            {
                return new Dictionary<int, MoodleCategory>();
            }

            // Generate IDs for all categories
            var idsToLoad = categoryList.Select(c => $"moodle-categories/{c.Id}").Distinct().ToList();

            // Single batch load of all existing documents
            var existingDocs = await _serviceRavenDb.AsyncSession.LoadAsync<MoodleCategory>(idsToLoad, cancellationToken);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var result = new Dictionary<int, MoodleCategory>();

            foreach (var cat in categoryList)
            {
                var id = $"moodle-categories/{cat.Id}";
                var existing = existingDocs.TryGetValue(id, out var doc) ? doc : null;

                if (existing != null)
                {
                    // Update existing in memory - RavenDB tracks changes automatically
                    if (!string.Equals(existing.Name, cat.Name, StringComparison.Ordinal))
                    {
                        existing.Name = cat.Name;
                    }
                    if (existing.ParentId != cat.Parent)
                    {
                        existing.ParentId = cat.Parent;
                    }
                    if (existing.Depth != cat.Depth)
                    {
                        existing.Depth = cat.Depth;
                    }
                    if (!string.Equals(existing.Path, cat.Path, StringComparison.Ordinal))
                    {
                        existing.Path = cat.Path;
                    }
                    existing.LastSyncedAt = now;

                    result[cat.Id] = existing;
                }
                else
                {
                    // Create new category
                    var newCategory = new MoodleCategory
                    {
                        Id = id,
                        MoodleId = cat.Id,
                        Name = cat.Name,
                        ParentId = cat.Parent,
                        Depth = cat.Depth,
                        Path = cat.Path,
                        LastSyncedAt = now
                    };

                    await _serviceRavenDb.AsyncSession.StoreAsync(newCategory, id, cancellationToken);
                    result[cat.Id] = newCategory;
                }
            }

            // RavenDB tracks all changes automatically, SaveChangesAsync called by caller/session management
            return result;
        }

        #endregion

        #region Category Hierarchy Methods

        public async Task<IReadOnlyCollection<MoodleCategory>> GetCategoriesByDepthAsync(int depth, CancellationToken cancellationToken = default)
        {
            var categories = await _serviceRavenDb.AsyncSession.Query<MoodleCategory, MoodleCategories_ByDepthAndParent>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(c => c.Depth == depth)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return categories;
        }

        public Task<IReadOnlyCollection<MoodleCategory>> GetInstitutionsAsync(CancellationToken cancellationToken = default)
        {
            // Depth 1 = Instituição (SENAI, SESI, etc.)
            return GetCategoriesByDepthAsync(1, cancellationToken);
        }

        public async Task<IReadOnlyCollection<MoodleCategory>> GetSchoolsByInstitutionAsync(int institutionMoodleId, CancellationToken cancellationToken = default)
        {
            // Depth 2 = Escola, com ParentId = institutionMoodleId
            var categories = await _serviceRavenDb.AsyncSession.Query<MoodleCategory, MoodleCategories_ByDepthAndParent>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(c => c.Depth == 2 && c.ParentId == institutionMoodleId)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return categories;
        }

        public async Task<IReadOnlyCollection<MoodleCategory>> GetCoursesBySchoolAsync(int schoolMoodleId, CancellationToken cancellationToken = default)
        {
            // Depth 3 = Curso, com ParentId = schoolMoodleId
            var categories = await _serviceRavenDb.AsyncSession.Query<MoodleCategory, MoodleCategories_ByDepthAndParent>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(c => c.Depth == 3 && c.ParentId == schoolMoodleId)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return categories;
        }

        public async Task<IReadOnlyCollection<MoodleCategory>> GetEventsByCourseAsync(int courseMoodleId, CancellationToken cancellationToken = default)
        {
            // Depth 4 = Evento/Turma, com ParentId = courseMoodleId
            var categories = await _serviceRavenDb.AsyncSession.Query<MoodleCategory, MoodleCategories_ByDepthAndParent>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(c => c.Depth == 4 && c.ParentId == courseMoodleId)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return categories;
        }

        public async Task<MoodleCategoryHierarchy> GetCategoryHierarchyAsync(string? path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return new MoodleCategoryHierarchy();
            }

            // Extrair IDs do path
            var pathIds = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

            if (pathIds.Count == 0)
            {
                return new MoodleCategoryHierarchy();
            }

            // Buscar todas as categorias do path em uma única operação
            var categories = await GetCategoriesByMoodleIdsAsync(pathIds, cancellationToken);

            return MoodleCategoryHierarchy.FromPath(path, categories);
        }

        #endregion
    }
}
