using JobTracker.Data;
using JobTracker.Services;
using System.Security.Claims;

namespace JobTracker.Middleware
{
    public class TenantContextMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, JobTrackerContext dbContext)
        {
            var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                var user = await dbContext.Users.FindAsync(userId);
                if (user != null)
                {
                    tenantContext.SetCurrentCompanyId(user.CompanyId);
                }
            }

            await _next(context);
        }
    }
}
