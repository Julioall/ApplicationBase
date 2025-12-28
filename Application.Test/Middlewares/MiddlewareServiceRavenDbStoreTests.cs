using Application.Api.Middlewares;
using Application.Domain.Model;
using Application.Infrastructure.ConfigurationDb;
using Application.Infrastructure.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.TestDriver;

namespace Application.Test.Middlewares
{
    public class MiddlewareServiceRavenDbStoreTests : RavenTestDriver
    {
        private sealed class TestRavenService : IServiceRavenDB, IAsyncDisposable
        {
            public IDocumentStore Store { get; set; }
            public Raven.Client.Documents.Session.IDocumentSession Session { get; set; }
            public Raven.Client.Documents.Session.IAsyncDocumentSession AsyncSession { get; set; }
            public bool SessionDisposed { get; private set; }
            public bool AsyncSessionDisposed { get; private set; }

            public ValueTask DisposeAsync()
            {
                SessionDisposed = SessionDisposed || Session != null;
                AsyncSessionDisposed = AsyncSessionDisposed || AsyncSession != null;
                return ValueTask.CompletedTask;
            }
        }

        [Fact]
        public async Task Should_Open_Save_And_Dispose_Sessions_Per_Request()
        {
            var store = GetDocumentStore();
            Environment.SetEnvironmentVariable(ApplicationConstants.DATABASE_NAME_KEY, store.Database);

            var services = new ServiceCollection();
            services.AddSingleton<IDocumentStore>(store);
            services.AddScoped<IServiceRavenDB, TestRavenService>();

            var provider = services.BuildServiceProvider();
            var middleware = new MiddlewareServiceRavenDbStore(_ => Task.CompletedTask);
            var scope = provider.CreateScope();
            var context = new DefaultHttpContext
            {
                RequestServices = scope.ServiceProvider
            };

            var documentStore = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
            await middleware.Invoke(context, scope.ServiceProvider.GetRequiredService<IServiceRavenDB>(), documentStore);

            var ravenService = (TestRavenService)scope.ServiceProvider.GetRequiredService<IServiceRavenDB>();
            Assert.NotNull(ravenService.Session);
            Assert.NotNull(ravenService.AsyncSession);
            Assert.True(ravenService.SessionDisposed);
        }

        [Fact]
        public async Task Should_Dispose_Sessions_When_Next_Throws()
        {
            var store = GetDocumentStore();
            var services = new ServiceCollection();
            services.AddSingleton<IDocumentStore>(store);
            services.AddScoped<IServiceRavenDB, TestRavenService>();

            var provider = services.BuildServiceProvider();
            var scope = provider.CreateScope();
            var context = new DefaultHttpContext
            {
                RequestServices = scope.ServiceProvider
            };

            var middleware = new MiddlewareServiceRavenDbStore(_ => throw new InvalidOperationException("boom"));
            var documentStore = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context, scope.ServiceProvider.GetRequiredService<IServiceRavenDB>(), documentStore));

            var ravenService = (TestRavenService)scope.ServiceProvider.GetRequiredService<IServiceRavenDB>();
            Assert.True(ravenService.SessionDisposed);
            Assert.True(ravenService.AsyncSessionDisposed);
        }
    }
}
