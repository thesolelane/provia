using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Services
{
    public class DatabaseSeeder
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(JobTrackerContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Check if company already exists
                var existingCompany = await _context.Companies.FirstOrDefaultAsync();
                if (existingCompany != null)
                {
                    _logger.LogInformation("Database already seeded");
                    return;
                }

                // Create Preferred Builders company
                var company = new Company
                {
                    CompanyName = "Preferred Builders USA, LLC",
                    AccountNumber = "36DMRD",
                    ContactEmail = "admin@preferredbuildersusa.com",
                    ContactPhone = "(978) 320-1714",
                    Address = "Massachusetts, USA",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                // Create admin user
                var adminUser = new User
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@preferredbuildersusa.com",
                    Username = "admin",
                    PasswordHash = "admin123", // Simple for demo - will be hashed in production
                    Role = UserRoles.Admin,
                    CompanyId = company.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LanguagePreference = "en"
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                // Create supervisors
                var supervisors = new[]
                {
                    new User
                    {
                        FirstName = "Tony",
                        LastName = "Cooper",
                        Email = "anthonycooper1967@gmail.com",
                        Username = "tony.cooper",
                        PasswordHash = "supervisor123",
                        Role = UserRoles.Supervisor,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = adminUser.Id,
                        LanguagePreference = "en"
                    },
                    new User
                    {
                        FirstName = "Erika",
                        LastName = "Silva",
                        Email = "erika.silva@preferredbuildersusa.com",
                        Username = "erika.silva",
                        PasswordHash = "supervisor123",
                        Role = UserRoles.Supervisor,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = adminUser.Id,
                        LanguagePreference = "en"
                    },
                    new User
                    {
                        FirstName = "Fabio",
                        LastName = "Silva",
                        Email = "fabio.lago@preferredbuildersusa.com",
                        Username = "fabio.silva",
                        PasswordHash = "supervisor123",
                        Role = UserRoles.Supervisor,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = adminUser.Id,
                        LanguagePreference = "en"
                    },
                    new User
                    {
                        FirstName = "Anthony",
                        LastName = "Cooper",
                        Email = "realestatebyawc@gmail.com",
                        Username = "anthony.cooper",
                        PasswordHash = "supervisor123",
                        Role = UserRoles.Supervisor,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = adminUser.Id,
                        LanguagePreference = "en"
                    }
                };

                _context.Users.AddRange(supervisors);
                await _context.SaveChangesAsync();

                // Create field operators
                var fieldOperators = new[]
                {
                    new User
                    {
                        FirstName = "Jackson",
                        LastName = "Deaquino",
                        Email = "jackson.deaquino@preferredbuildersusa.com",
                        PhoneNumber = "(978) 320-1715",
                        PinHash = "1234",
                        Role = UserRoles.FieldOperator,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = adminUser.Id,
                        LanguagePreference = "en"
                    },
                    new User
                    {
                        FirstName = "Anthony",
                        LastName = "Cooper",
                        Email = "cooper@preferredbuildersusa.com",
                        PhoneNumber = "(978) 320-1716",
                        PinHash = "1234",
                        Role = UserRoles.FieldOperator,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = adminUser.Id,
                        LanguagePreference = "en"
                    },
                    new User
                    {
                        FirstName = "Mike",
                        LastName = "Johnson",
                        Email = "mike.johnson@preferredbuildersusa.com",
                        PhoneNumber = "(978) 320-1714",
                        PinHash = "1234",
                        Role = UserRoles.FieldOperator,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = adminUser.Id,
                        LanguagePreference = "en"
                    }
                };

                _context.Users.AddRange(fieldOperators);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Database seeded successfully with Preferred Builders team");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database");
                throw;
            }
        }
    }
}