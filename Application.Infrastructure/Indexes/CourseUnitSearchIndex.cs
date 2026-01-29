using Application.Domain.Model.Education;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    public class CourseUnitSearchIndex : AbstractIndexCreationTask<ClassUcMap, CourseUnitSearchIndex.Result>
    {
        public class Result
        {
            public string CourseUnitId { get; set; } = string.Empty;
            public string ClassId { get; set; } = string.Empty;
            public string ProgramId { get; set; } = string.Empty;
            public string SchoolId { get; set; } = string.Empty;
            public string Fullname { get; set; } = string.Empty;
            public long StartDate { get; set; }
            public long EndDate { get; set; }
        }

        public CourseUnitSearchIndex()
        {
            Map = maps => from map in maps
                          let courseUnit = LoadDocument<CourseUnit>(map.CourseUnitId)
                          let turma = LoadDocument<ClassDocument>(map.ClassId)
                          select new Result
                          {
                              CourseUnitId = map.CourseUnitId,
                              ClassId = map.ClassId,
                              ProgramId = turma != null ? turma.ProgramId : string.Empty,
                              SchoolId = turma != null ? turma.SchoolId : string.Empty,
                              Fullname = courseUnit != null ? courseUnit.Fullname : string.Empty,
                              StartDate = courseUnit != null ? courseUnit.StartDate : 0,
                              EndDate = courseUnit != null ? courseUnit.EndDate : 0
                          };

            Index(x => x.Fullname, FieldIndexing.Search);
            StoreAllFields(FieldStorage.Yes);
        }
    }
}
