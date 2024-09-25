using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Embedded;
using System;

namespace Application.Test.Setup
{
    public abstract class TestHelper : IDisposable
    {
        protected ServiceProvider ServiceProvider;

        protected TestHelper()
        {
            ServiceProvider = GetServiceCollection().BuildServiceProvider();
        }

        private IServiceCollection GetServiceCollection()
        {
            var services = new ServiceCollection();
            DependencyInjectionModule.BindService(services);
            return services;
        }

        public virtual void Dispose()
        {
            ServiceProvider.GetRequiredService<IDocumentStore>()?.Dispose();
            EmbeddedServer.Instance.Dispose();
            ServiceProvider.Dispose();
        }
    }
}
