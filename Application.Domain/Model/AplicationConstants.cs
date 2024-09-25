namespace Application.Domain.Model
{
    public static class AplicationConstants
    {
        public static readonly string DATABASE_URLS = Environment.GetEnvironmentVariable("RAVENDBSETTINGS_URLS");
        public static readonly string DATABASE_NAME = Environment.GetEnvironmentVariable("RAVENDBSETTINGS_DATABASE_NAME");
        public static readonly string CERTIFICATE_SUBJECT = Environment.GetEnvironmentVariable("RAVENDBSETTINGS_CERTIFICATE_SUBJECT");

        public static readonly string JWT_ISSUER = Environment.GetEnvironmentVariable("JWT_ISSUER");
        public static readonly string JWT_AUDIENCE = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        public static readonly string JWT_SIGNING_KEY = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");
    }
}
