using System;

namespace Application.Api.RateLimiting
{
    public class RateLimitSettings
    {
        public int RegistrationPerIpLimit { get; set; } = 5;
        public int RegistrationPerEmailLimit { get; set; } = 5;
        public int RegistrationWindowMinutes { get; set; } = 15;

        public int RecoveryGeneratePerIpLimit { get; set; } = 10;
        public int RecoveryGeneratePerEmailLimit { get; set; } = 5;
        public int RecoveryGenerateWindowMinutes { get; set; } = 30;

        public int RecoveryVerifyPerIpLimit { get; set; } = 10;
        public int RecoveryVerifyPerEmailLimit { get; set; } = 5;
        public int RecoveryVerifyWindowMinutes { get; set; } = 30;

        public TimeSpan RegistrationWindow => TimeSpan.FromMinutes(RegistrationWindowMinutes);
        public TimeSpan RecoveryGenerateWindow => TimeSpan.FromMinutes(RecoveryGenerateWindowMinutes);
        public TimeSpan RecoveryVerifyWindow => TimeSpan.FromMinutes(RecoveryVerifyWindowMinutes);
    }
}
