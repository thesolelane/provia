using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace JobTracker.Services
{
    public class SimpleAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public SimpleAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                if (!Request.Headers.ContainsKey("Authorization"))
                {
                    return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
                }

                var authHeader = Request.Headers["Authorization"].ToString();
                if (!authHeader.StartsWith("Bearer "))
                {
                    return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header"));
                }

                var token = authHeader.Substring("Bearer ".Length);
                
                // Simple token validation - decode the base64 token
                try
                {
                    var tokenBytes = Convert.FromBase64String(token);
                    var tokenPayload = Encoding.UTF8.GetString(tokenBytes);
                    var parts = tokenPayload.Split(':');
                    
                    if (parts.Length < 4)
                    {
                        return Task.FromResult(AuthenticateResult.Fail("Invalid token format"));
                    }

                    var userId = parts[0];
                    var email = parts[1];
                    var role = parts[2];
                    var timestamp = parts[3];

                    // Create claims for the authenticated user
                    var claims = new[]
                    {
                        new Claim("userId", userId),
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Role, role),
                        new Claim("timestamp", timestamp)
                    };

                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);

                    return Task.FromResult(AuthenticateResult.Success(ticket));
                }
                catch (Exception)
                {
                    return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Authentication error");
                return Task.FromResult(AuthenticateResult.Fail("Authentication error"));
            }
        }
    }
}