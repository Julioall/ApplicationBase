using Application.Domain.Model.Moodle;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    public class StudentCourseMaps_ByCourse : AbstractIndexCreationTask<StudentCourseMap>
    {
        public StudentCourseMaps_ByCourse()
        {
            Map = maps => from map in maps
                          select new
                          {
                              map.CourseId,
                              map.StudentId
                          };
        }
    }
}
