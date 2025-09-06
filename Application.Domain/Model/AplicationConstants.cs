namespace Application.Domain.Model
{
    public static class AplicationConstants
    {
        public static readonly string DATABASE_URL = Environment.GetEnvironmentVariable("DATABASE_URL");
        public static readonly string DATABASE_NAME = Environment.GetEnvironmentVariable("DATABASE_NAME");
        public static readonly string CERTIFICATE_SUBJECT = Environment.GetEnvironmentVariable("CERTIFICATE_SUBJECT");

        public static readonly string JWT_ISSUER = Environment.GetEnvironmentVariable("JWT_ISSUER");
        public static readonly string JWT_AUDIENCE = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        public static readonly string JWT_SIGNING_KEY = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");
    }
}
