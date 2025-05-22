using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using JobTrackerApp.Models;

namespace JobTrackerApp.Services
{
    public class ActiveDirectoryService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ActiveDirectoryService> _logger;

        public ActiveDirectoryService(IConfiguration configuration, ILogger<ActiveDirectoryService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public bool ValidateCredentials(string username, string password)
        {
            try
            {
                // Check if we're using mock authentication for development
                if (_configuration.GetValue<bool>("ActiveDirectory:UseMock", false))
                {
                    string mockAdmin = _configuration["ActiveDirectory:MockAdmin"] ?? "admin@dev.com";
                    string mockPassword = _configuration["ActiveDirectory:MockPassword"] ?? "Admin123!";
                    return username == mockAdmin && password == mockPassword;
                }

                // Real AD authentication
                string domain = _configuration["ActiveDirectory:Domain"] ?? throw new InvalidOperationException("AD Domain not configured");
                
                using (var context = new PrincipalContext(ContextType.Domain, domain))
                {
                    return context.ValidateCredentials(username, password);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating AD credentials for user {Username}", username);
                return false;
            }
        }

        public User? GetUserDetails(string username)
        {
            try
            {
                // Check if we're using mock authentication for development
                if (_configuration.GetValue<bool>("ActiveDirectory:UseMock", false))
                {
                    if (username == (_configuration["ActiveDirectory:MockAdmin"] ?? "admin@dev.com"))
                    {
                        return new User
                        {
                            Username = username,
                            Email = username,
                            ActiveDirectoryId = "mock_ad_id",
                            Role = "Admin",
                            IsActive = true,
                            LastLogin = DateTime.UtcNow
                        };
                    }
                    return null;
                }

                // Real AD user lookup
                string domain = _configuration["ActiveDirectory:Domain"] ?? throw new InvalidOperationException("AD Domain not configured");
                string ldapPath = _configuration["ActiveDirectory:LdapPath"] ?? throw new InvalidOperationException("LDAP Path not configured");
                
                using (var context = new PrincipalContext(ContextType.Domain, domain))
                {
                    UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, username);
                    
                    if (userPrincipal == null)
                    {
                        return null;
                    }

                    var user = new User
                    {
                        Username = userPrincipal.SamAccountName,
                        Email = userPrincipal.EmailAddress,
                        ActiveDirectoryId = userPrincipal.Guid.ToString(),
                        IsActive = userPrincipal.Enabled ?? true,
                        LastLogin = DateTime.UtcNow
                    };

                    // Get additional properties from AD if needed
                    using (var entry = new DirectoryEntry($"{ldapPath}/CN={userPrincipal.Name}"))
                    {
                        // Get group memberships to determine role
                        var groups = userPrincipal.GetGroups();
                        foreach (var group in groups)
                        {
                            if (group.Name.Contains("JobTracker_Admin"))
                            {
                                user.Role = "Admin";
                                break;
                            }
                            else if (group.Name.Contains("JobTracker_ProjectManager"))
                            {
                                user.Role = "ProjectManager";
                                break;
                            }
                            else if (group.Name.Contains("JobTracker_Employee"))
                            {
                                user.Role = "Employee";
                                break;
                            }
                            else
                            {
                                user.Role = "User";
                            }
                        }
                    }

                    return user;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AD user details for {Username}", username);
                return null;
            }
        }

        public string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "defaultKeyThatShouldBeReplaced"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString()),
                new Claim("EmployeeId", user.EmployeeId?.ToString() ?? "0")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"] ?? "60")),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
