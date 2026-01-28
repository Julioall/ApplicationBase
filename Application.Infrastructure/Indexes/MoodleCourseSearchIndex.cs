using Application.Domain.Model.Moodle;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    public class MoodleCourseSearchIndex : AbstractIndexCreationTask<MoodleCohortCourseMap, MoodleCourseSearchIndex.Result>
    {
        public class Result
        {
            public string CourseId { get; set; } = string.Empty;
            public string CohortId { get; set; } = string.Empty;
            public string CourseCategoryId { get; set; } = string.Empty;
            public string CategoryId { get; set; } = string.Empty;
            public string Fullname { get; set; } = string.Empty;
            public long StartDate { get; set; }
            public long EndDate { get; set; }
        }

        public MoodleCourseSearchIndex()
        {
            Map = maps => from map in maps
                          let course = LoadDocument<MoodleCourse>(map.CourseId)
                          let cohort = LoadDocument<MoodleCohort>(map.CohortId)
                          select new Result
                          {
                              CourseId = map.CourseId,
                              CohortId = map.CohortId,
                              CourseCategoryId = cohort != null ? cohort.CourseCategoryId : string.Empty,
                              CategoryId = cohort != null ? cohort.CategoryId : string.Empty,
                              Fullname = course != null ? course.Fullname : string.Empty,
                              StartDate = course != null ? course.StartDate : 0,
                              EndDate = course != null ? course.EndDate : 0
                          };

            Index(x => x.Fullname, FieldIndexing.Search);
            StoreAllFields(FieldStorage.Yes);
        }
    }
}
