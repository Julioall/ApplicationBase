using Application.Domain.Interface;
using Application.Domain.Model.User;
using Application.Domain;
using Application.Infrastructure;
using Application.Infrastructure.Repository;
using Application.Service;
using Application.Service.Interface;
using Application.Service.Service;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Embedded;
using Raven.TestDriver;
using Application.Domain.Validator;

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
            InitializeDataBase();
            _serviceCollection = InicializeServices();

            _serviceCollection.AddScoped<IServiceRavenDB>((provider) => new ServiceRavenDB
            {
                Store = _store,
                Session = _session,
                AsyncSession = _asyncSession
            });

            _serviceCollection.AddScoped<IUserService, UserService>();
            _serviceCollection.AddScoped<IValidator<User>, UserValidator>();
            _serviceCollection.AddScoped<IUserRepository, UserRepository>();

            _serviceProvider = _serviceCollection.BuildServiceProvider();
        }

        private static ServiceCollection InicializeServices()
        {
            var serviceCollection = new ServiceCollection();
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
