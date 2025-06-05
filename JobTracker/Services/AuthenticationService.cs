using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JobTracker.Services
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> AuthenticateAsync(string identifier, string credential);
        Task<AuthenticationResult> AuthenticatePhoneAsync(string phoneNumber, string pin);
        Task<User?> GetUserByTokenAsync(string token);
        Task<bool> ValidateTokenAsync(string token);
        string GenerateJwtToken(User user);
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public User? User { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly JobTrackerContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            JobTrackerContext context, 
            IConfiguration configuration,
            ILogger<AuthenticationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string identifier, string credential)
        {
            try
            {
                // Find user by email or username
                var user = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => 
                        u.Email == identifier || 
                        u.Username == identifier);

                if (user == null)
                {
                    _logger.LogWarning($"Authentication failed: User not found for identifier {identifier}");
                    return new AuthenticationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Invalid credentials" 
                    };
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning($"Authentication failed: User {user.Id} is inactive");
                    return new AuthenticationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Account is inactive" 
                    };
                }

                // Verify password
                if (!VerifyPassword(credential, user.PasswordHash))
                {
                    _logger.LogWarning($"Authentication failed: Invalid password for user {user.Id}");
                    return new AuthenticationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Invalid credentials" 
                    };
                }

                // Update last login
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);

                _logger.LogInformation($"User {user.Id} authenticated successfully");

                return new AuthenticationResult
                {
                    Success = true,
                    Token = token,
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during authentication for identifier {identifier}");
                return new AuthenticationResult 
                { 
                    Success = false, 
                    ErrorMessage = "Authentication error occurred" 
                };
            }
        }

        public async Task<AuthenticationResult> AuthenticatePhoneAsync(string phoneNumber, string pin)
        {
            try
            {
                // Find user by phone number
                var user = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

                if (user == null)
                {
                    _logger.LogWarning($"Phone authentication failed: User not found for phone {phoneNumber}");
                    return new AuthenticationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Invalid phone number or PIN" 
                    };
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning($"Phone authentication failed: User {user.Id} is inactive");
                    return new AuthenticationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Account is inactive" 
                    };
                }

                // Verify PIN (for field operators)
                if (!VerifyPin(pin, user.PinHash))
                {
                    _logger.LogWarning($"Phone authentication failed: Invalid PIN for user {user.Id}");
                    return new AuthenticationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Invalid phone number or PIN" 
                    };
                }

                // Update last login
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);

                _logger.LogInformation($"User {user.Id} authenticated successfully via phone");

                return new AuthenticationResult
                {
                    Success = true,
                    Token = token,
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during phone authentication for phone {phoneNumber}");
                return new AuthenticationResult 
                { 
                    Success = false, 
                    ErrorMessage = "Authentication error occurred" 
                };
            }
        }

        public string GenerateJwtToken(User user)
        {
            var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "JobTracker-Default-Secret-Key-2024-Super-Secure-Development";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("userId", user.Id.ToString()),
                new Claim("email", user.Email ?? ""),
                new Claim("role", user.Role.ToString()),
                new Claim("companyId", user.CompanyId.ToString()),
                new Claim("firstName", user.FirstName ?? ""),
                new Claim("lastName", user.LastName ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, 
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                    ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: "JobTracker",
                audience: "JobTracker-Users",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8), // 8 hour expiry
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User?> GetUserByTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "JobTracker-Default-Secret-Key-2024-Super-Secure-Development";
                var key = Encoding.UTF8.GetBytes(jwtKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = "JobTracker",
                    ValidateAudience = true,
                    ValidAudience = "JobTracker-Users",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                var userIdClaim = principal.FindFirst("userId");

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return await _context.Users
                        .Include(u => u.Company)
                        .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating token");
                return null;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            var user = await GetUserByTokenAsync(token);
            return user != null;
        }

        private bool VerifyPassword(string password, string? hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            try
            {
                // For development, use simple comparison if no proper hash exists
                if (hash.Length < 50)
                {
                    return password == hash;
                }

                // In production, use proper password hashing verification
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        private bool VerifyPin(string pin, string? hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            try
            {
                // For development, use simple comparison
                if (hash.Length <= 10)
                {
                    return pin == hash;
                }

                // In production, use proper hashing
                return BCrypt.Net.BCrypt.Verify(pin, hash);
            }
            catch
            {
                return false;
            }
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static string HashPin(string pin)
        {
            return BCrypt.Net.BCrypt.HashPassword(pin);
        }
    }
}