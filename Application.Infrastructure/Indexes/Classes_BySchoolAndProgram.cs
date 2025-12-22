using Application.Domain.Model.Education;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    public class Classes_BySchoolAndProgram : AbstractIndexCreationTask<ClassDocument>
    {
        public Classes_BySchoolAndProgram()
        {
            Map = classes => from c in classes
                             select new
                             {
                                 c.SchoolId,
                                 c.ProgramId,
                                 c.Name,
                                 c.StartDate,
                                 c.EndDate
                             };
        }
    }
}
