namespace Application.Domain.Model
{
    public static class ApplicationConstants
    {
        // RavenDB
        public const string DATABASE_URL_KEY = "RAVENDBSETTINGS_URLS";
        public const string DATABASE_NAME_KEY = "RAVENDBSETTINGS_DATABASE_NAME";
        public const string CERTIFICATE_SUBJECT_KEY = "RAVENDBSETTINGS_CERTIFICATE_SUBJECT";

        public static string? DATABASE_URL => Environment.GetEnvironmentVariable(DATABASE_URL_KEY);
        public static string? DATABASE_NAME => Environment.GetEnvironmentVariable(DATABASE_NAME_KEY);
        public static string? CERTIFICATE_SUBJECT => Environment.GetEnvironmentVariable(CERTIFICATE_SUBJECT_KEY);

        public static string? JWT_ISSUER => Environment.GetEnvironmentVariable("JWT_ISSUER");
        public static string? JWT_AUDIENCE => Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        public static string? JWT_SIGNING_KEY => Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");
    }
}
