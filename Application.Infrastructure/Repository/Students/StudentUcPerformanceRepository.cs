using Application.Domain.Interface;
using Application.Domain.Model.Education;
using Application.Infrastructure.Service;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System.Linq;

namespace Application.Infrastructure.Repository.Students
{
    public class StudentUcPerformanceRepository : IStudentUcPerformanceRepository
    {
        private readonly IServiceRavenDB _serviceRavenDb;

        public StudentUcPerformanceRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task<StudentUcPerformance?> GetByStudentAndUcAsync(string studentId, string ucId)
        {
            var result = await _serviceRavenDb.AsyncSession
                .Query<StudentUcPerformance>()
                .Where(p => p.StudentId == studentId && p.UcId == ucId)
                .FirstOrDefaultAsync();
            
            return result;
        }

        public async Task<IEnumerable<string>> GetUcsByStudentAsync(string studentId)
        {
            var ucs = await _serviceRavenDb.AsyncSession
                .Query<StudentUcPerformance>()
                .Where(p => p.StudentId == studentId)
                .Select(p => p.UcId)
                .Distinct()
                .ToListAsync();
            
            return ucs;
        }

        public async Task<IEnumerable<StudentUcPerformance>> GetStudentUcPerformanceAsync(string studentId)
        {
            var performances = await _serviceRavenDb.AsyncSession
                .Query<StudentUcPerformance>()
                .Where(p => p.StudentId == studentId)
                .ToListAsync();
            
            return performances;
        }

        public async Task SaveAsync(StudentUcPerformance performance)
        {
            ArgumentNullException.ThrowIfNull(performance);
            
            // Deixar RavenDB gerenciar o ID automaticamente
            // Se o objeto não tem ID, RavenDB vai gerar um novo
            // Se tem ID, vai usar o existente
            await _serviceRavenDb.AsyncSession.StoreAsync(performance);
        }

        public async Task UpdateAsync(StudentUcPerformance performance)
        {
            ArgumentNullException.ThrowIfNull(performance);
            await _serviceRavenDb.AsyncSession.StoreAsync(performance);
        }

        /// <summary>
        /// Carrega múltiplos desempenhos por studentId e ucId
        /// Agrupa por studentId para evitar comparações entre campos no LINQ
        /// </summary>
        public async Task<Dictionary<string, StudentUcPerformance>> GetByStudentAndUcBatchAsync(List<(string StudentId, string UcId)> pairs)
        {
            if (!pairs.Any())
                return new Dictionary<string, StudentUcPerformance>();

            // Agrupar pairs por StudentId para fazer queries mais eficientes
            var groupedByStudent = pairs
                .GroupBy(p => p.StudentId)
                .ToList();

            var result = new Dictionary<string, StudentUcPerformance>();

            // Para cada grupo de studentId, fazer uma query
            foreach (var studentGroup in groupedByStudent)
            {
                var studentId = studentGroup.Key;
                var ucIds = studentGroup.Select(p => p.UcId).ToHashSet();

                // Query: Buscar todos os desempenhos deste student que estão na lista de ucIds
                var performances = await _serviceRavenDb.AsyncSession
                    .Query<StudentUcPerformance>()
                    .Where(p => p.StudentId == studentId && p.UcId.In(ucIds))
                    .ToListAsync();

                // Adicionar ao resultado com chave composta
                foreach (var perf in performances)
                {
                    var key = $"{perf.StudentId}:{perf.UcId}";
                    result[key] = perf;
                }
            }

            return result;
        }

        /// <summary>
        /// Carrega todos os desempenhos de um único estudante
        /// Método útil para cache inicial
        /// </summary>
        public async Task<Dictionary<string, StudentUcPerformance>> GetAllByStudentAsync(string studentId)
        {
            var performances = await _serviceRavenDb.AsyncSession
                .Query<StudentUcPerformance>()
                .Where(p => p.StudentId == studentId)
                .ToListAsync();

            var result = new Dictionary<string, StudentUcPerformance>();
            foreach (var perf in performances)
            {
                var key = $"{perf.StudentId}:{perf.UcId}";
                result[key] = perf;
            }
            return result;
        }

        /// <summary>
        /// Carrega configuração de atividades ocultas para uma UC
        /// </summary>
        public async Task<HiddenActivitiesConfig?> GetHiddenActivitiesAsync(string ucId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ucId);
            
            var config = await _serviceRavenDb.AsyncSession
                .Query<HiddenActivitiesConfig>()
                .Where(c => c.UcId == ucId)
                .FirstOrDefaultAsync();
            
            return config;
        }

        /// <summary>
        /// Salva configuração de atividades ocultas para uma UC
        /// </summary>
        public async Task SaveHiddenActivitiesAsync(HiddenActivitiesConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentException.ThrowIfNullOrWhiteSpace(config.UcId);
            
            // Usar ID consistente baseado no UcId
            var id = $"hidden-activities/{config.UcId}";
            await _serviceRavenDb.AsyncSession.StoreAsync(config, id);
        }
    }
}
