namespace Application.Domain.Model
{
    public static class ApplicationPermissions
    {
        public const string PermissionClaimType = "permissions";

        public const string ViewHome = "view:home";
        public const string ViewProfile = "view:profile";
        public const string ManageUsers = "manage:users";
        public const string ManageServices = "manage:services";
        public const string ManageEmail = "manage:email";
        public const string ManageWhatsApp = "manage:whatsapp";
        public const string ManageWhatsAppSelf = "manage:whatsapp-self";
        public const string ViewStudents = "view:students";
        public const string ManageStudents = "manage:students";
        public const string ViewEducation = "view:education";
        public const string ManageEducation = "manage:education";

        public static IReadOnlyCollection<string> DefaultUserPermissions => new[]
        {
            ViewHome,
            ViewProfile,
            ManageServices,
            ManageWhatsAppSelf,
            ViewEducation,
            ManageEducation
        };

        public static IReadOnlyCollection<string> DefaultAdminPermissions => new[]
        {
            ViewHome,
            ViewProfile,
            ManageUsers,
            ManageServices,
            ManageEmail,
            ManageWhatsApp,
            ManageWhatsAppSelf,
            ViewStudents,
            ManageStudents,
            ViewEducation,
            ManageEducation
        };

        public static IReadOnlyCollection<string> All => new[]
        {
            ViewHome,
            ViewProfile,
            ManageUsers,
            ManageServices,
            ManageEmail,
            ManageWhatsApp,
            ManageWhatsAppSelf,
            ViewStudents,
            ManageStudents,
            ViewEducation,
            ManageEducation
        };
    }
}
