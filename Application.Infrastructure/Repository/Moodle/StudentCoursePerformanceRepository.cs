using Application.Domain.Interface.Moodle;
using Application.Domain.Model.Moodle;
using Application.Infrastructure.Service;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace Application.Infrastructure.Repository.Moodle
{
    public class StudentCoursePerformanceRepository : IStudentCoursePerformanceRepository
    {
        private readonly IServiceRavenDB _serviceRavenDb;

        public StudentCoursePerformanceRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task<StudentCoursePerformance?> GetByStudentAndCourseAsync(string studentId, string courseId)
        {
            var result = await _serviceRavenDb.AsyncSession
                .Query<StudentCoursePerformance>()
                .Where(p => p.StudentId == studentId && p.CourseId == courseId)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<IEnumerable<string>> GetCoursesByStudentAsync(string studentId)
        {
            var courses = await _serviceRavenDb.AsyncSession
                .Query<StudentCoursePerformance>()
                .Where(p => p.StudentId == studentId)
                .Select(p => p.CourseId)
                .Distinct()
                .ToListAsync();

            return courses;
        }

        public async Task<IEnumerable<StudentCoursePerformance>> GetStudentCoursePerformanceAsync(string studentId)
        {
            var performances = await _serviceRavenDb.AsyncSession
                .Query<StudentCoursePerformance>()
                .Where(p => p.StudentId == studentId)
                .ToListAsync();

            return performances;
        }

        public async Task SaveAsync(StudentCoursePerformance performance)
        {
            ArgumentNullException.ThrowIfNull(performance);
            await _serviceRavenDb.AsyncSession.StoreAsync(performance);
        }

        public async Task UpdateAsync(StudentCoursePerformance performance)
        {
            ArgumentNullException.ThrowIfNull(performance);
            await _serviceRavenDb.AsyncSession.StoreAsync(performance);
        }

        /// <summary>
        /// Loads multiple performances by studentId and courseId.
        /// Groups by studentId to avoid field comparisons in LINQ.
        /// </summary>
        public async Task<Dictionary<string, StudentCoursePerformance>> GetByStudentAndCourseBatchAsync(List<(string StudentId, string CourseId)> pairs)
        {
            if (!pairs.Any())
                return new Dictionary<string, StudentCoursePerformance>();

            var groupedByStudent = pairs
                .GroupBy(p => p.StudentId)
                .ToList();

            var result = new Dictionary<string, StudentCoursePerformance>();

            foreach (var studentGroup in groupedByStudent)
            {
                var studentId = studentGroup.Key;
                var courseIds = studentGroup.Select(p => p.CourseId).ToHashSet();

                var performances = await _serviceRavenDb.AsyncSession
                    .Query<StudentCoursePerformance>()
                    .Where(p => p.StudentId == studentId && p.CourseId.In(courseIds))
                    .ToListAsync();

                foreach (var perf in performances)
                {
                    var key = $"{perf.StudentId}:{perf.CourseId}";
                    result[key] = perf;
                }
            }

            return result;
        }

        /// <summary>
        /// Loads all performances for a single student.
        /// Useful for initial cache.
        /// </summary>
        public async Task<Dictionary<string, StudentCoursePerformance>> GetAllByStudentAsync(string studentId)
        {
            var performances = await _serviceRavenDb.AsyncSession
                .Query<StudentCoursePerformance>()
                .Where(p => p.StudentId == studentId)
                .ToListAsync();

            var result = new Dictionary<string, StudentCoursePerformance>();
            foreach (var perf in performances)
            {
                var key = $"{perf.StudentId}:{perf.CourseId}";
                result[key] = perf;
            }
            return result;
        }

        /// <summary>
        /// Loads hidden activities configuration for a course.
        /// </summary>
        public async Task<HiddenActivitiesConfig?> GetHiddenActivitiesAsync(string courseId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(courseId);

            var config = await _serviceRavenDb.AsyncSession
                .Query<HiddenActivitiesConfig>()
                .Where(c => c.CourseId == courseId)
                .FirstOrDefaultAsync();

            return config;
        }

        /// <summary>
        /// Saves hidden activities configuration for a course.
        /// </summary>
        public async Task SaveHiddenActivitiesAsync(HiddenActivitiesConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentException.ThrowIfNullOrWhiteSpace(config.CourseId);

            var id = $"hidden-activities/{config.CourseId}";
            await _serviceRavenDb.AsyncSession.StoreAsync(config, id);
        }
    }
}
