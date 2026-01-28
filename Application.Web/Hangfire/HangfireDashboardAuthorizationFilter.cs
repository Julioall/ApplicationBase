using Application.Domain.Model;
using Hangfire.Dashboard;

namespace Application.Api.Hangfire;

public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly bool _allowAnonymous;

    public HangfireDashboardAuthorizationFilter(bool allowAnonymous = false)
    {
        _allowAnonymous = allowAnonymous;
    }

    public bool Authorize(DashboardContext context)
    {
        // Allow anonymous access in development
        if (_allowAnonymous)
        {
            return true;
        }

        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return user.Claims.Any(claim =>
            claim.Type == ApplicationPermissions.PermissionClaimType &&
            string.Equals(claim.Value, ApplicationPermissions.ManageServices, StringComparison.OrdinalIgnoreCase));
    }
}
