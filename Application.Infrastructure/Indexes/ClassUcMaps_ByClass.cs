using Application.Domain.Model.Education;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    public class ClassUcMaps_ByClass : AbstractIndexCreationTask<ClassUcMap>
    {
        public ClassUcMaps_ByClass()
        {
            Map = maps => from map in maps
                          select new
                          {
                              map.ClassId,
                              map.UcId,
                              map.ImportedAt
                          };
        }
    }
}
