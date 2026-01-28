using Application.Domain.Model.Moodle;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    public class MoodleCohorts_ByCategoryAndCourseCategory : AbstractIndexCreationTask<MoodleCohort>
    {
        public MoodleCohorts_ByCategoryAndCourseCategory()
        {
            Map = cohorts => from c in cohorts
                             select new
                             {
                                 c.CategoryId,
                                 c.CourseCategoryId,
                                 c.Name,
                                 c.StartDate,
                                 c.EndDate
                             };
        }
    }
}
