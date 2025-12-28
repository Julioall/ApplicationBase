using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Application.Infrastructure.Service;

/// <summary>
/// Abstração para sessões RavenDB utilizada pela infraestrutura.
/// </summary>
public interface IServiceRavenDB
{
    IDocumentStore Store { get; set; }
    IDocumentSession Session { get; set; }
    IAsyncDocumentSession AsyncSession { get; set; }
}
