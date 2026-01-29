using Application.Domain.Interface;
using Application.Domain.Model.Education;
using Application.Infrastructure.Service;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System.Linq;

namespace Application.Infrastructure.Repository.Students
{
    public class StudentCourseUnitPerformanceRepository : IStudentUcPerformanceRepository
    {
        private readonly IServiceRavenDB _serviceRavenDb;

        public StudentCourseUnitPerformanceRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task<StudentCourseUnitPerformance?> GetByStudentAndCourseUnitAsync(string studentId, string courseUnitId)
        {
            var result = await _serviceRavenDb.AsyncSession
                .Query<StudentCourseUnitPerformance>()
                .Where(p => p.StudentId == studentId && p.CourseUnitId == courseUnitId)
                .FirstOrDefaultAsync();
            
            return result;
        }

        public async Task<IEnumerable<string>> GetCourseUnitsByStudentAsync(string studentId)
        {
            var courseUnits = await _serviceRavenDb.AsyncSession
                .Query<StudentCourseUnitPerformance>()
                .Where(p => p.StudentId == studentId)
                .Select(p => p.CourseUnitId)
                .Distinct()
                .ToListAsync();
            
            return courseUnits;
        }

        public async Task<IEnumerable<StudentCourseUnitPerformance>> GetStudentCourseUnitPerformanceAsync(string studentId)
        {
            var performances = await _serviceRavenDb.AsyncSession
                .Query<StudentCourseUnitPerformance>()
                .Where(p => p.StudentId == studentId)
                .ToListAsync();
            
            return performances;
        }

        public async Task SaveAsync(StudentCourseUnitPerformance performance)
        {
            ArgumentNullException.ThrowIfNull(performance);
            
            // Deixar RavenDB gerenciar o ID automaticamente
            // Se o objeto não tem ID, RavenDB vai gerar um novo
            // Se tem ID, vai usar o existente
            await _serviceRavenDb.AsyncSession.StoreAsync(performance);
        }

        public async Task UpdateAsync(StudentCourseUnitPerformance performance)
        {
            ArgumentNullException.ThrowIfNull(performance);
            await _serviceRavenDb.AsyncSession.StoreAsync(performance);
        }

        /// <summary>
        /// Carrega múltiplos desempenhos por studentId e ucId
        /// Agrupa por studentId para evitar comparações entre campos no LINQ
        /// </summary>
        public async Task<Dictionary<string, StudentCourseUnitPerformance>> GetByStudentAndCourseUnitBatchAsync(List<(string StudentId, string CourseUnitId)> pairs)
        {
            if (!pairs.Any())
                return new Dictionary<string, StudentCourseUnitPerformance>();

            // Agrupar pairs por StudentId para fazer queries mais eficientes
            var groupedByStudent = pairs
                .GroupBy(p => p.StudentId)
                .ToList();

            var result = new Dictionary<string, StudentCourseUnitPerformance>();

            // Para cada grupo de studentId, fazer uma query
            foreach (var studentGroup in groupedByStudent)
            {
                var studentId = studentGroup.Key;
                var courseUnitIds = studentGroup.Select(p => p.CourseUnitId).ToHashSet();

                // Query: Buscar todos os desempenhos deste student que estão na lista de ucIds
                var performances = await _serviceRavenDb.AsyncSession
                    .Query<StudentCourseUnitPerformance>()
                    .Where(p => p.StudentId == studentId && p.CourseUnitId.In(courseUnitIds))
                    .ToListAsync();

                // Adicionar ao resultado com chave composta
                foreach (var perf in performances)
                {
                    var key = $"{perf.StudentId}:{perf.CourseUnitId}";
                    result[key] = perf;
                }
            }

            return result;
        }

        /// <summary>
        /// Carrega todos os desempenhos de um único estudante
        /// Método útil para cache inicial
        /// </summary>
        public async Task<Dictionary<string, StudentCourseUnitPerformance>> GetAllByStudentAsync(string studentId)
        {
            var performances = await _serviceRavenDb.AsyncSession
                .Query<StudentCourseUnitPerformance>()
                .Where(p => p.StudentId == studentId)
                .ToListAsync();

            var result = new Dictionary<string, StudentCourseUnitPerformance>();
            foreach (var perf in performances)
            {
                var key = $"{perf.StudentId}:{perf.CourseUnitId}";
                result[key] = perf;
            }
            return result;
        }

        /// <summary>
        /// Carrega configuração de atividades ocultas para uma CourseUnit
        /// </summary>
        public async Task<HiddenActivitiesConfig?> GetHiddenActivitiesAsync(string courseUnitId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(courseUnitId);
            
            var config = await _serviceRavenDb.AsyncSession
                .Query<HiddenActivitiesConfig>()
                .Where(c => c.CourseUnitId == courseUnitId)
                .FirstOrDefaultAsync();
            
            return config;
        }

        /// <summary>
        /// Salva configuração de atividades ocultas para uma CourseUnit
        /// </summary>
        public async Task SaveHiddenActivitiesAsync(HiddenActivitiesConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentException.ThrowIfNullOrWhiteSpace(config.CourseUnitId);
            
            // Usar ID consistente baseado no UcId
            var id = $"hidden-activities/{config.CourseUnitId}";
            await _serviceRavenDb.AsyncSession.StoreAsync(config, id);
        }
    }
}
