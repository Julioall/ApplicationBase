namespace Application.Domain.Model
{
    public static class ApplicationPermissions
    {
        public const string PermissionClaimType = "permissions";

        public const string ViewHome = "view:home";
        public const string ViewProfile = "view:profile";
        public const string ManageUsers = "manage:users";

        public static IReadOnlyCollection<string> DefaultUserPermissions => new[]
        {
            ViewHome,
            ViewProfile
        };

        public static IReadOnlyCollection<string> DefaultAdminPermissions => new[]
        {
            ViewHome,
            ViewProfile,
            ManageUsers
        };

        public static IReadOnlyCollection<string> All => new[]
        {
            ViewHome,
            ViewProfile,
            ManageUsers
        };
    }
}
