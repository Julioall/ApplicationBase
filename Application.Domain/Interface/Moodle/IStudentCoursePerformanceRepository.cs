using Application.Domain.Model.Moodle;

namespace Application.Domain.Interface.Moodle
{
    public interface IStudentCoursePerformanceRepository
    {
        Task<StudentCoursePerformance?> GetByStudentAndCourseAsync(string studentId, string courseId);
        Task<IEnumerable<string>> GetCoursesByStudentAsync(string studentId);
        Task<IEnumerable<StudentCoursePerformance>> GetStudentCoursePerformanceAsync(string studentId);
        Task SaveAsync(StudentCoursePerformance performance);
        Task UpdateAsync(StudentCoursePerformance performance);
        Task<Dictionary<string, StudentCoursePerformance>> GetByStudentAndCourseBatchAsync(List<(string StudentId, string CourseId)> pairs);
        Task<Dictionary<string, StudentCoursePerformance>> GetAllByStudentAsync(string studentId);
        Task<HiddenActivitiesConfig?> GetHiddenActivitiesAsync(string courseId);
        Task SaveHiddenActivitiesAsync(HiddenActivitiesConfig config);
    }
}
