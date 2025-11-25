using Microsoft.Extensions.Caching.Memory;

namespace JobTracker.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _memoryCache;
        private const int MaxLoginAttempts = 5;
        private const int LockoutDurationSeconds = 900; // 15 minutes

        public RateLimitingMiddleware(RequestDelegate next, IMemoryCache memoryCache)
        {
            _next = next;
            _memoryCache = memoryCache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Apply rate limiting to login endpoint
            if (context.Request.Path.StartsWithSegments("/api/WorkingAuth/login"))
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var cacheKey = $"login_attempts_{ipAddress}";

                if (_memoryCache.TryGetValue(cacheKey, out int attempts))
                {
                    if (attempts >= MaxLoginAttempts)
                    {
                        context.Response.StatusCode = 429; // Too Many Requests
                        await context.Response.WriteAsJsonAsync(new { message = "Too many login attempts. Try again later." });
                        return;
                    }
                }

                var currentAttempts = attempts > 0 ? attempts : 0;
                _memoryCache.Set(cacheKey, currentAttempts + 1, TimeSpan.FromSeconds(LockoutDurationSeconds));
            }

            await _next(context);
        }
    }
}
