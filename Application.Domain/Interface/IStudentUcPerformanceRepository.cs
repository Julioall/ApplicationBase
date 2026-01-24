using Application.Domain.Model.Education;

namespace Application.Domain.Interface
{
    public interface IStudentUcPerformanceRepository
    {
        Task<StudentUcPerformance?> GetByStudentAndUcAsync(string studentId, string ucId);
        Task<IEnumerable<string>> GetUcsByStudentAsync(string studentId);
        Task<IEnumerable<StudentUcPerformance>> GetStudentUcPerformanceAsync(string studentId);
        Task SaveAsync(StudentUcPerformance performance);
        Task UpdateAsync(StudentUcPerformance performance);
        Task<Dictionary<string, StudentUcPerformance>> GetByStudentAndUcBatchAsync(List<(string StudentId, string UcId)> pairs);
        Task<Dictionary<string, StudentUcPerformance>> GetAllByStudentAsync(string studentId);
        Task<HiddenActivitiesConfig?> GetHiddenActivitiesAsync(string ucId);
        Task SaveHiddenActivitiesAsync(HiddenActivitiesConfig config);
    }
}
