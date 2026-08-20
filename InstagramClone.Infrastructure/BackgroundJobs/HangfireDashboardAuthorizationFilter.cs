using Hangfire.Dashboard;

namespace InstagramClone.Infrastructure.BackgroundJobs;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In local/development or when auth is handled, allow dashboard access.
        // For production, check admin role / authentication:
        // var httpContext = context.GetHttpContext();
        // return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("Administrator");
        return true;
    }
}
