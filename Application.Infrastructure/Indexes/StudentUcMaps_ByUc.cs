using Application.Domain.Model.Education;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    public class StudentUcMaps_ByUc : AbstractIndexCreationTask<StudentUcMap>
    {
        public StudentUcMaps_ByUc()
        {
            Map = maps => from map in maps
                          select new
                          {
                              map.UcId,
                              map.StudentId,
                              map.ImportedAt
                          };
        }
    }
}
