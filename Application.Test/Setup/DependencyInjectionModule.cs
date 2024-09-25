using Application.Domain.Interface;
using Application.Infrastructure.Repository;
using Application.Service.Interface;
using Application.Service.Service;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Embedded;

namespace Application.Test.Setup
{
    public static class DependencyInjectionModule
    {
        public static void BindService(ServiceCollection services)
        {
            var embeddedServer = EmbeddedServer.Instance;
            embeddedServer.StartServer(new ServerOptions
            {
                DataDirectory = "C:\\RavenData",
                ServerUrl = "http://127.0.0.1:8080"
            });

            var documentStore = embeddedServer.GetDocumentStore("EmbeddedDatabase");
            services.AddSingleton<IDocumentStore>(documentStore);
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserRepository, UserRepositoryRavenDB>();
        }
    }
}
