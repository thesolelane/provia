using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Services
{
    public class DatabaseSeeder
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;
        private readonly ICodeEngineService _codeEngineService;

        public DatabaseSeeder(JobTrackerContext context, ILogger<DatabaseSeeder> logger, ICodeEngineService codeEngineService)
        {
            _context = context;
            _logger = logger;
            _codeEngineService = codeEngineService;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Check if CodeRules need seeding (separate from company seeding)
                var existingRules = await _context.CodeRules.AnyAsync();
                if (!existingRules)
                {
                    _logger.LogInformation("Seeding Code Engine (CodeBooks and CodeRules)");
                    await _codeEngineService.SeedMassachusettsCodesAsync();
                    _logger.LogInformation("Seeded Massachusetts Code Engine");

                    _logger.LogInformation("Seeding Scope Categories and Items");
                    await _codeEngineService.SeedScopeCategoriesAsync();
                    _logger.LogInformation("Seeded Scope Categories and Items");
                }

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
                    ContactEmail = "contact@preferredbuildersusa.com",
                    ContactPhone = "(978) 320-1714",
                    Address = "Massachusetts, USA",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Companies.Add(company);
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
                        Role = 1520,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = null,
                        LanguagePreference = "en"
                    },
                    new User
                    {
                        FirstName = "Erika",
                        LastName = "Silva",
                        Email = "erika.silva@preferredbuildersusa.com",
                        Username = "erika.silva",
                        PasswordHash = "supervisor123",
                        Role = 1520,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = null,
                        LanguagePreference = "en"
                    },
                    new User
                    {
                        FirstName = "Fabio",
                        LastName = "Silva",
                        Email = "fabio.lago@preferredbuildersusa.com",
                        Username = "fabio.silva",
                        PasswordHash = "supervisor123",
                        Role = 1520,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = null,
                        LanguagePreference = "en"
                    },
                    new User
                    {
                        FirstName = "Anthony",
                        LastName = "Cooper",
                        Email = "realestatebyawc@gmail.com",
                        Username = "anthony.cooper",
                        PasswordHash = "supervisor123",
                        Role = 1520,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = null,
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
                        Role = 2001,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = null,
                        LanguagePreference = "en"
                    },
                    new User
                    {
                        FirstName = "Anthony",
                        LastName = "Cooper",
                        Email = "cooper@preferredbuildersusa.com",
                        PhoneNumber = "(978) 320-1716",
                        PinHash = "1234",
                        Role = 2001,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = null,
                        LanguagePreference = "en"
                    },
                    new User
                    {
                        FirstName = "Mike",
                        LastName = "Johnson",
                        Email = "mike.johnson@preferredbuildersusa.com",
                        PhoneNumber = "(978) 320-1714",
                        PinHash = "1234",
                        Role = 2001,
                        CompanyId = company.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = null,
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