using Application.Domain.Model;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using System.Security.Cryptography.X509Certificates;
using Application.Infrastructure.Indexes;
using Raven.Client.Documents.Indexes;
using Application.Domain.Localization;
using System.IO;
using System.Net.Http;
using System.Net.Security;

namespace Application.Infrastructure.ConfigurationDb
{
    public static class DocumentStoreHolderAlternative
    {
        public static IDocumentStore CreateStore(string? db = null)
        {
            db ??= Environment.GetEnvironmentVariable(ApplicationConstants.DATABASE_NAME_KEY);

            var url = Environment.GetEnvironmentVariable(ApplicationConstants.DATABASE_URL_KEY)
                ?? throw new Exception(SharedResourceProvider.GetString("EnvVarNotDefined", ApplicationConstants.DATABASE_URL_KEY));

            var urls = url.Split(',').ToArray();

            var certificate = GetCertificateFromStore();
            var documentStore = new DocumentStore
            {
                Urls = urls,
                Certificate = certificate,
                Conventions = GetConventions(),
                Database = db
            };

            if (certificate != null)
            {
                documentStore.Conventions.CreateHttpClient = handler =>
                {
                    handler.ServerCertificateCustomValidationCallback = (_, cert, _, errors) =>
                        errors == SslPolicyErrors.None || (cert != null && cert.Thumbprint == certificate.Thumbprint);
                    return new HttpClient(handler);
                };
            }

            documentStore.Initialize();
            CreateDatabaseIfDontExist(documentStore, db, true);
            IndexCreation.CreateIndexes(typeof(User_ByEmail).Assembly, documentStore);
            return documentStore;
        }

        private static X509Certificate2? GetCertificateFromStore()
        {
            var certificatePath = Environment.GetEnvironmentVariable(ApplicationConstants.CERTIFICATE_PATH_KEY);
            if (!string.IsNullOrWhiteSpace(certificatePath))
            {
                var certificatePassword = Environment.GetEnvironmentVariable(ApplicationConstants.CERTIFICATE_PASSWORD_KEY);
                if (string.IsNullOrWhiteSpace(certificatePassword))
                {
                    throw new Exception(SharedResourceProvider.GetString("EnvVarNotDefined", ApplicationConstants.CERTIFICATE_PASSWORD_KEY));
                }

                if (!File.Exists(certificatePath))
                {
                    throw new Exception(SharedResourceProvider.GetString("FileNotFound", certificatePath));
                }

                return new X509Certificate2(
                    certificatePath,
                    certificatePassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
            }

            var certificateSubject = Environment.GetEnvironmentVariable(ApplicationConstants.CERTIFICATE_SUBJECT_KEY);
            if (string.IsNullOrWhiteSpace(certificateSubject))
            {
                Console.Error.WriteLine(SharedResourceProvider.GetString("CertificateSubjectNotConfigured"));
                return null;
            }

            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, certificateSubject, false);
                if (certs.Count > 0)
                {
                    var certificate = certs.FirstOrDefault();
                    if (certificate is null)
                    {
                        throw new Exception(SharedResourceProvider.GetString("CertificateNotFound", certificateSubject));
                    }
                    if (!certificate.HasPrivateKey)
                    {
                        throw new Exception(SharedResourceProvider.GetString("CertificateMissingPrivateKey", certificateSubject));
                    }
                    return certificate;
                }
                else
                {
                    throw new Exception(SharedResourceProvider.GetString("CertificateNotFound", certificateSubject));
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

        public static void CreateDatabaseIfDontExist(IDocumentStore storeInstance, string? database = null, bool createDatabaseIfNotExists = true)
        {
            ArgumentNullException.ThrowIfNull(storeInstance);

            var dbName = string.IsNullOrWhiteSpace(database) ? storeInstance.Database : database;

            if (string.IsNullOrWhiteSpace(dbName))
            {
                throw new ArgumentException(SharedResourceProvider.GetString("DatabaseNameMissing"));
            }

            try
            {
                storeInstance.Maintenance.ForDatabase(dbName!).Send(new GetStatisticsOperation());
            }
            catch (DatabaseDoesNotExistException)
            {
                if (!createDatabaseIfNotExists)
                {
                    throw;
                }

                try
                {
                    var urls = Environment.GetEnvironmentVariable(ApplicationConstants.DATABASE_URL_KEY)
                        ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                    int count = urls?.Count ?? 0;
                    storeInstance.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(dbName), count == 0 ? 1 : count));
                }
                catch (ConcurrencyException)
                {
                }
            }
        }
    }
}
