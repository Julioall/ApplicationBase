using Application.Domain.Model.Education;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    public class UcSearchIndex : AbstractIndexCreationTask<ClassUcMap, UcSearchIndex.Result>
    {
        public class Result
        {
            public string UcId { get; set; } = string.Empty;
            public string ClassId { get; set; } = string.Empty;
            public string ProgramId { get; set; } = string.Empty;
            public string SchoolId { get; set; } = string.Empty;
            public string Fullname { get; set; } = string.Empty;
            public long StartDate { get; set; }
            public long EndDate { get; set; }
        }

        public UcSearchIndex()
        {
            Map = maps => from map in maps
                          let uc = LoadDocument<UcDocument>(map.UcId)
                          let turma = LoadDocument<ClassDocument>(map.ClassId)
                          select new Result
                          {
                              UcId = map.UcId,
                              ClassId = map.ClassId,
                              ProgramId = turma != null ? turma.ProgramId : string.Empty,
                              SchoolId = turma != null ? turma.SchoolId : string.Empty,
                              Fullname = uc != null ? uc.Fullname : string.Empty,
                              StartDate = uc != null ? uc.StartDate : 0,
                              EndDate = uc != null ? uc.EndDate : 0
                          };

            Index(x => x.Fullname, FieldIndexing.Search);
            StoreAllFields(FieldStorage.Yes);
        }
    }
}
