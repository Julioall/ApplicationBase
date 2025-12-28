using Application.Domain.Model;
using Hangfire.Dashboard;

namespace Application.Api.Hangfire;

public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
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
