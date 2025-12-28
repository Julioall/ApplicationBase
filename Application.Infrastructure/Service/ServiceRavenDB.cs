using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Application.Infrastructure.Service;

public class ServiceRavenDB : IServiceRavenDB
{
    public IDocumentStore Store { get; set; } = default!;
    public IDocumentSession Session { get; set; } = default!;
    public IAsyncDocumentSession AsyncSession { get; set; } = default!;
}
