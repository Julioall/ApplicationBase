using Application.Domain.Model.Moodle;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    /// <summary>
    /// Índice para consultar categorias do Moodle por Depth e ParentId.
    /// Otimiza consultas hierárquicas de Institution → School → Course → Event.
    /// </summary>
    public class MoodleCategories_ByDepthAndParent : AbstractIndexCreationTask<MoodleCategory>
    {
        public MoodleCategories_ByDepthAndParent()
        {
            Map = categories => from c in categories
                                select new
                                {
                                    c.MoodleId,
                                    c.Name,
                                    c.Depth,
                                    c.ParentId,
                                    c.Path,
                                    c.LastSyncedAt
                                };
        }
    }
}
