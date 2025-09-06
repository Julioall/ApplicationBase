using Application.Domain.Model;
using Application.Infrastructure.Persistence;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using System.Security.Cryptography.X509Certificates;

namespace Application.Infrastructure.ConfigurationDb
{
    public static class DocumentStoreHolderAlternative
    {
        private static Lazy<IDocumentStore> store = new Lazy<IDocumentStore>(InitializeStore);
        public static IDocumentStore Store => store.Value;
        public static bool StoreIsAlreadyCreated => store.IsValueCreated;

        private static IDocumentStore InitializeStore()
        {
            IDocumentStore documentStore = CreateStore();
            documentStore.Initialize();
            return documentStore;
        }

        public static IDocumentStore CreateStore(string? db = null)
        {
            db ??= AplicationConstants.DATABASE_NAME;


            var url = AplicationConstants.DATABASE_URL
                ?? throw new Exception("Environment variable [DATABASE_URL] is not defined");

            var urls = url.Split(',').ToArray();

            return new DocumentStore
            {
                Urls = urls,
                Certificate = GetCertificateFromStore(),
                Conventions = GetConventions(),
                Database = db
            };
        }

        private static X509Certificate2 GetCertificateFromStore()
        {
            if (AplicationConstants.CERTIFICATE_SUBJECT == null)
            {
                throw new InvalidOperationException("The environment variable RAVENDBSETTINGS_CERTIFICATE_SUBJECT has not been defined.");
            }

            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, AplicationConstants.CERTIFICATE_SUBJECT, false);
                if (certs.Count > 0)
                {
                    var certificate = certs.FirstOrDefault();
                    if (!certificate.HasPrivateKey)
                    {
                        throw new Exception($"Certificate with subject '{AplicationConstants.CERTIFICATE_SUBJECT}' does not have a private key.");
                    }
                    return certificate;
                }
                else
                {
                    throw new Exception($"Certificate with subject '{AplicationConstants.CERTIFICATE_SUBJECT}' not found in the LocalMachine certificate store.");
                }
            }
        }

        private static DocumentConventions GetConventions()
        {
            return new DocumentConventions
            {
                MaxNumberOfRequestsPerSession = 30,
                UseOptimisticConcurrency = true,
                SaveEnumsAsIntegers = true,
                IdentityPartsSeparator = '-'
            };
        }

        public static void CreateDatabaseIfDontExist(string? database = null, bool createDatabaseIfNotExists = true)
        {
            if (database == null)
            {
                database = Store.Database;
            }

            if (string.IsNullOrWhiteSpace(database))
            {
                throw new ArgumentException("Create database dont find definition to database name");
            }

            try
            {
                Store.Maintenance.ForDatabase(database).Send(new GetStatisticsOperation());
            }
            catch (DatabaseDoesNotExistException)
            {
                if (!createDatabaseIfNotExists)
                {
                    throw;
                }

                try
                {
                    int count = Environment.GetEnvironmentVariable(RavenDbConstants.DATABASE_URL).Split(',').ToList().Count;
                    Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(database), count == 0m ? 1 : count));
                }
                catch (ConcurrencyException)
                {
                }
            }
        }
    }
}
