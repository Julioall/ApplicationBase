using Application.Domain;
using Application.Domain.Interface;
using Application.Infrastructure;
using Application.Infrastructure.Indexes;
using Application.Infrastructure.Service;
using Application.Service;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Embedded;
using Raven.Client.Documents.Indexes;
using Raven.TestDriver;
using System.Globalization;

namespace Application.Tests.Setup
{
    public class BaseTest : RavenTestDriver
    {
        public readonly ServiceProvider _serviceProvider;
        public readonly IServiceCollection _serviceCollection;

        public IDocumentStore _store { get; set; }
        public IDocumentSession _session { get; set; }
        public IAsyncDocumentSession _asyncSession { get; set; }

        private static bool _isAlreadyConfigured = false;

        public BaseTest()
        {
            var culture = new CultureInfo("pt");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            InitializeDataBase();
            _serviceCollection = InicializeServices(_store, _session, _asyncSession);

            _serviceProvider = _serviceCollection.BuildServiceProvider();
        }

        private static ServiceCollection InicializeServices(IDocumentStore documentStore, IDocumentSession session, IAsyncDocumentSession asyncSession)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLocalization(options => options.ResourcesPath = "Resources");
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<IDocumentStore>(documentStore);
            serviceCollection.AddScoped<IServiceRavenDB>(_ => new ServiceRavenDB
            {
                Store = documentStore,
                Session = session,
                AsyncSession = asyncSession
            });
            serviceCollection.AddInfraDependencies();
            serviceCollection.AddServiceDependencies();
            serviceCollection.AddDomainDependencies();

            return serviceCollection;
        }

        private void InitializeDataBase()
        {
            if (!_isAlreadyConfigured)
            {
                ConfigureServer(new TestServerOptions
                {
                    Licensing = new ServerOptions.LicensingOptions
                    {
                        ThrowOnInvalidOrMissingLicense = false
                    }
                });

                _isAlreadyConfigured = true;
            }

            _store = GetDocumentStore(null, Guid.NewGuid().ToString());
            IndexCreation.CreateIndexes(typeof(User_ByEmail).Assembly, _store);
            _session = _store.OpenSession();
            _asyncSession = _store.OpenAsyncSession();
        }

        protected override void PreInitialize(IDocumentStore documentStore)
        {
            base.PreInitialize(documentStore);
            documentStore.Conventions.IdentityPartsSeparator = '-';
        }
    }
}
