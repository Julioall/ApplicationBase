using Application.Domain.Model.Education;

namespace Application.Domain.Interface
{
    public interface IStudentUcPerformanceRepository
    {
        Task<StudentCourseUnitPerformance?> GetByStudentAndCourseUnitAsync(string studentId, string courseUnitId);
        Task<IEnumerable<string>> GetCourseUnitsByStudentAsync(string studentId);
        Task<IEnumerable<StudentCourseUnitPerformance>> GetStudentCourseUnitPerformanceAsync(string studentId);
        Task SaveAsync(StudentCourseUnitPerformance performance);
        Task UpdateAsync(StudentCourseUnitPerformance performance);
        Task<Dictionary<string, StudentCourseUnitPerformance>> GetByStudentAndCourseUnitBatchAsync(List<(string StudentId, string CourseUnitId)> pairs);
        Task<Dictionary<string, StudentCourseUnitPerformance>> GetAllByStudentAsync(string studentId);
        Task<HiddenActivitiesConfig?> GetHiddenActivitiesAsync(string courseUnitId);
        Task SaveHiddenActivitiesAsync(HiddenActivitiesConfig config);
    }
}
