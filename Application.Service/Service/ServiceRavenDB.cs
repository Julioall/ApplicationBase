using Application.Domain.Interface;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Application.Service.Service;

public class ServiceRavenDB : IServiceRavenDB
{
    public IDocumentStore Store { get; set; }
    public IDocumentSession Session { get; set; }
    public IAsyncDocumentSession AsyncSession { get; set; }
}