using Application.Domain.Interface;
using Application.Domain.Model;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using System.Security.Cryptography.X509Certificates;

namespace Application.Infrastructure.Repository
{
    public static class RavenDBConfiguration
    {
        public static void AddRavenDB(this IServiceCollection services)
        {
            // Uncomment this section if you want to use a certificate.
            // var certificate = GetCertificateFromStore();

            if (string.IsNullOrEmpty(AplicationConstants.DATABASE_URLS))
            {
                throw new ApplicationException("The RavenDB URL has not been configured correctly. Check the environment variable 'RAVENDBSETTINGS_URLS'.");
            }

            if (string.IsNullOrEmpty(AplicationConstants.DATABASE_NAME))
            {
                throw new ApplicationException("The name of the RavenDB database has not been configured correctly. Check the environment variable 'RAVENDBSETTINGS_DATABASE_NAME'.");
            }

            // Uncomment this section if you want to use a certificate.
            // if (certificate == null)
            // {
            //     throw new ApplicationException("The RavenDB certificate has not been configured correctly. Ensure the certificate is installed in the LocalMachine certificate store.");
            // }

            var documentStore = new DocumentStore
            {
                Urls = new[] { AplicationConstants.DATABASE_URLS },
                Database = AplicationConstants.DATABASE_NAME,
                // Uncomment this section if you want to use a certificate.
                // Certificate = certificate,
            };

            documentStore.Initialize();

            services.AddSingleton<IDocumentStore>(documentStore);
            services.AddScoped<IUserRepository, UserRepositoryRavenDB>();
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
    }
}
