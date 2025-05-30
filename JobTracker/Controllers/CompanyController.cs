using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using System.Security.Cryptography;
using System.Text;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(JobTrackerContext context, ILogger<CompanyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterCompany([FromBody] CompanyRegistrationRequest request)
        {
            try
            {
                // Check if company email already exists
                var existingCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.ContactEmail == request.ContactEmail);

                if (existingCompany != null)
                {
                    return BadRequest(new { message = "A company with this email address already exists" });
                }

                // Generate unique account number
                var accountNumber = await GenerateUniqueAccountNumber();

                // Set subscription limits based on type
                var (maxUsers, maxJobs, canUseAdvanced) = GetSubscriptionLimits(request.RequestedSubscription);

                // Create company
                var company = new Company
                {
                    AccountNumber = accountNumber,
                    CompanyName = request.CompanyName,
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    Address = request.Address,
                    City = request.City,
                    State = request.State,
                    ZipCode = request.ZipCode,
                    SubscriptionType = request.RequestedSubscription,
                    SubscriptionStartDate = DateTime.UtcNow,
                    SubscriptionEndDate = request.RequestedSubscription == SubscriptionType.Trial 
                        ? DateTime.UtcNow.AddDays(30) 
                        : null,
                    MaxUsers = maxUsers,
                    MaxJobs = maxJobs,
                    CanUseAdvancedFeatures = canUseAdvanced,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                // Create first Master Admin for the company
                var masterAdmin = new User
                {
                    FirstName = request.AdminFirstName,
                    LastName = request.AdminLastName,
                    Email = request.ContactEmail,
                    Role = UserRoles.MasterAdmin,
                    LanguagePreference = request.LanguagePreference,
                    PasswordHash = HashPassword(request.AdminPassword),
                    CompanyId = company.Id,
                    IsActive = true,
                    IsEmailVerified = true, // Auto-verify for initial admin
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(masterAdmin);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Company registered successfully!",
                    accountNumber = accountNumber,
                    companyId = company.Id,
                    subscription = request.RequestedSubscription.ToString(),
                    trialExpiry = company.SubscriptionEndDate,
                    limits = new
                    {
                        maxUsers,
                        maxJobs,
                        advancedFeatures = canUseAdvanced
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Company registration error");
                return StatusCode(500, new { message = "Company registration failed" });
            }
        }

        [HttpGet("info/{accountNumber}")]
        public async Task<IActionResult> GetCompanyInfo(string accountNumber)
        {
            try
            {
                var company = await _context.Companies
                    .Include(c => c.Users)
                    .Include(c => c.Jobs)
                    .FirstOrDefaultAsync(c => c.AccountNumber == accountNumber && c.IsActive);

                if (company == null)
                {
                    return NotFound(new { message = "Company not found" });
                }

                var userCount = company.Users.Count(u => u.IsActive);
                var jobCount = company.Jobs.Count();

                return Ok(new
                {
                    accountNumber = company.AccountNumber,
                    companyName = company.CompanyName,
                    subscription = company.SubscriptionType.ToString(),
                    isTrialExpired = company.IsTrialExpired,
                    subscriptionEndDate = company.SubscriptionEndDate,
                    usage = new
                    {
                        users = new { current = userCount, max = company.MaxUsers },
                        jobs = new { current = jobCount, max = company.MaxJobs }
                    },
                    features = new
                    {
                        advancedFeatures = company.CanUseAdvancedFeatures,
                        multiLanguage = true,
                        apiAccess = company.SubscriptionType != SubscriptionType.Trial
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company info for account: {AccountNumber}", accountNumber);
                return StatusCode(500, new { message = "Failed to get company information" });
            }
        }

        [HttpGet("validate-account/{accountNumber}")]
        public async Task<IActionResult> ValidateAccount(string accountNumber)
        {
            try
            {
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.AccountNumber == accountNumber);

                if (company == null)
                {
                    return Ok(new { isValid = false, message = "Account number not found" });
                }

                if (!company.IsActive)
                {
                    return Ok(new { isValid = false, message = "Account is inactive" });
                }

                if (company.IsTrialExpired)
                {
                    return Ok(new { isValid = false, message = "Trial subscription has expired" });
                }

                return Ok(new { 
                    isValid = true, 
                    companyName = company.CompanyName,
                    subscription = company.SubscriptionType.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating account: {AccountNumber}", accountNumber);
                return StatusCode(500, new { message = "Account validation failed" });
            }
        }

        private async Task<string> GenerateUniqueAccountNumber()
        {
            string accountNumber;
            bool isUnique;

            do
            {
                // Generate format: YYYY-MMDD-XXXX (Year-MonthDay-RandomNumber)
                var today = DateTime.UtcNow;
                var randomNumber = new Random().Next(1000, 9999);
                accountNumber = $"{today.Year}-{today.Month:D2}{today.Day:D2}-{randomNumber}";

                isUnique = !await _context.Companies.AnyAsync(c => c.AccountNumber == accountNumber);
            } while (!isUnique);

            return accountNumber;
        }

        private static (int maxUsers, int maxJobs, bool canUseAdvanced) GetSubscriptionLimits(SubscriptionType subscription)
        {
            return subscription switch
            {
                SubscriptionType.Trial => (5, 10, false),
                SubscriptionType.Basic => (15, 50, false),
                SubscriptionType.Professional => (50, 200, true),
                SubscriptionType.Enterprise => (int.MaxValue, int.MaxValue, true),
                _ => (5, 10, false)
            };
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "JobTracker_Company_Salt"));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}