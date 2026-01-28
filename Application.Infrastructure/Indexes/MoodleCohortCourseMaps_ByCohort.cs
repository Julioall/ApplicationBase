using Application.Domain.Model.Moodle;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    public class MoodleCohortCourseMaps_ByCohort : AbstractIndexCreationTask<MoodleCohortCourseMap>
    {
        public MoodleCohortCourseMaps_ByCohort()
        {
            Map = maps => from map in maps
                          select new
                          {
                              map.CohortId,
                              map.CourseId
                          };
        }
    }
}
