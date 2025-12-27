namespace Application.Domain.Model
{
    public static class ApplicationConstants
    {
        // RavenDB
        public const string DATABASE_URL_KEY = "RAVENDBSETTINGS_URLS";
        public const string DATABASE_NAME_KEY = "RAVENDBSETTINGS_DATABASE_NAME";
        public const string CERTIFICATE_SUBJECT_KEY = "RAVENDBSETTINGS_CERTIFICATE_SUBJECT";
        public const string CERTIFICATE_PATH_KEY = "RAVENDBSETTINGS_CERTIFICATE_PATH";
        public const string CERTIFICATE_PASSWORD_KEY = "RAVENDBSETTINGS_CERTIFICATE_PASSWORD";

        // JWT
        public const string JWT_ISSUER_KEY = "JWT_ISSUER";
        public const string JWT_AUDIENCE_KEY = "JWT_AUDIENCE";
        public const string JWT_SIGNING_KEY = "JWT_SIGNING_KEY";

        // Secrets
        public const string SECRET_ENCRYPTION_KEY = "APP_SECRET_ENCRYPTION_KEY";
    }
}
