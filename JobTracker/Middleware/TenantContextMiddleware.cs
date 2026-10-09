using JobTracker.Data;
using JobTracker.Services;
using Microsoft.EntityFrameworkCore;
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
                var user = await dbContext.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null && user.IsActive && user.CompanyId > 0 && user.Company?.IsActive == true)
                {
                    tenantContext.SetCurrentCompanyId(user.CompanyId);
                    tenantContext.UserId = user.Id;
                }
            }

            await _next(context);
        }
    }
}
