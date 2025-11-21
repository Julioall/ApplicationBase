using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Application.Infrastructure.Interface
{
    /// <summary>
    /// Infra provider for RavenDB sessions (fora do domínio).
    /// </summary>
    public interface IServiceRavenDB
    {
        IDocumentStore Store { get; set; }
        IDocumentSession Session { get; set; }
        IAsyncDocumentSession AsyncSession { get; set; }
    }
}
