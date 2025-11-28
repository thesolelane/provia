using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobTracker.Services
{
    /// <summary>
    /// Code Engine Service - Auto-generates permits and inspections from scope selections
    /// Based on Massachusetts jurisdiction codes (780 CMR, 248 CMR, 527 CMR, etc.)
    /// </summary>
    public interface ICodeEngineService
    {
        Task<List<ScopeCategoryDto>> GetAllScopeCategoriesAsync();
        Task<List<CodeRuleDto>> GetCodeRulesForScopeAsync(string scopeItemCode);
        Task<bool> SaveJobScopesAsync(int jobId, int companyId, List<int> scopeItemIds, int createdByUserId);
        Task<List<InspectionStage>> GenerateInspectionsFromScopesAsync(int jobId, int companyId);
        Task<List<string>> GetRequiredPermitTypesAsync(int jobId);
        Task<object> GetDepartmentViewAsync(int jobId, string department);
        Task SeedMassachusettsCodesAsync();
        Task SeedScopeCategoriesAsync();
    }

    public class CodeEngineService : ICodeEngineService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<CodeEngineService> _logger;

        public CodeEngineService(JobTrackerContext context, ILogger<CodeEngineService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all scope categories with items for Job Options Wizard
        /// </summary>
        public async Task<List<ScopeCategoryDto>> GetAllScopeCategoriesAsync()
        {
            var categories = await _context.ScopeCategories
                .Where(c => c.IsActive)
                .Include(c => c.ScopeItems!.Where(i => i.IsActive))
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return categories.Select(c => new ScopeCategoryDto
            {
                Id = c.Id,
                CategoryCode = c.CategoryCode,
                CategoryName = c.CategoryName,
                Description = c.Description,
                Items = c.ScopeItems?.Select(i => new ScopeItemDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    Description = i.Description,
                    TradeType = i.TradeType,
                    RequiresLicensedTrade = i.RequiresLicensedTrade,
                    Department = i.Department
                }).OrderBy(i => i.Id).ToList() ?? new List<ScopeItemDto>()
            }).ToList();
        }

        /// <summary>
        /// Get code rules triggered by a specific scope item
        /// </summary>
        public async Task<List<CodeRuleDto>> GetCodeRulesForScopeAsync(string scopeItemCode)
        {
            var rules = await _context.CodeRules
                .Where(r => r.IsActive && r.TriggerScope == scopeItemCode)
                .Include(r => r.CodeBook)
                .ToListAsync();

            return rules.Select(r => new CodeRuleDto
            {
                Id = r.Id,
                RuleName = r.RuleName,
                TriggerScope = r.TriggerScope,
                RequiredPermitType = r.RequiredPermitType,
                RequiredInspectionType = r.RequiredInspectionType,
                CodeSection = r.CodeSection,
                Description = r.Description,
                Department = r.Department,
                CodeBookName = r.CodeBook?.Name ?? "",
                CodeBookCode = r.CodeBook?.Code ?? ""
            }).ToList();
        }

        /// <summary>
        /// Save selected scopes for a job (from Job Options Wizard)
        /// </summary>
        public async Task<bool> SaveJobScopesAsync(int jobId, int companyId, List<int> scopeItemIds, int createdByUserId)
        {
            try
            {
                // Remove existing scopes for this job
                var existingScopes = await _context.JobScopes
                    .Where(js => js.JobId == jobId)
                    .ToListAsync();
                _context.JobScopes.RemoveRange(existingScopes);

                // Add new scopes
                foreach (var scopeItemId in scopeItemIds)
                {
                    var jobScope = new JobScope
                    {
                        JobId = jobId,
                        ScopeItemId = scopeItemId,
                        CompanyId = companyId,
                        Status = "PENDING",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.JobScopes.Add(jobScope);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Saved {scopeItemIds.Count} scopes for job {jobId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving job scopes");
                return false;
            }
        }

        /// <summary>
        /// Auto-generate inspection stages based on selected scopes
        /// </summary>
        public async Task<List<InspectionStage>> GenerateInspectionsFromScopesAsync(int jobId, int companyId)
        {
            try
            {
                // Get all scope items for this job
                var jobScopes = await _context.JobScopes
                    .Where(js => js.JobId == jobId)
                    .Include(js => js.ScopeItem)
                    .ToListAsync();

                if (!jobScopes.Any())
                    return new List<InspectionStage>();

                // Get all scope codes
                var scopeCodes = jobScopes
                    .Where(js => js.ScopeItem != null)
                    .Select(js => js.ScopeItem!.ItemCode)
                    .ToList();

                // Find matching code rules
                var codeRules = await _context.CodeRules
                    .Where(r => r.IsActive && scopeCodes.Contains(r.TriggerScope))
                    .Include(r => r.CodeBook)
                    .OrderBy(r => r.InspectionOrder)
                    .ToListAsync();

                // Get existing JobBid for this job (if any)
                var jobBid = await _context.JobBids
                    .FirstOrDefaultAsync(b => b.JobId == jobId && b.Status == "ACCEPTED");

                var createdInspections = new List<InspectionStage>();

                // Group rules by inspection type to avoid duplicates
                var inspectionGroups = codeRules
                    .Where(r => !string.IsNullOrEmpty(r.RequiredInspectionType))
                    .GroupBy(r => r.RequiredInspectionType)
                    .ToList();

                foreach (var group in inspectionGroups)
                {
                    var rule = group.First();
                    
                    // Create inspection stage
                    var inspection = new InspectionStage
                    {
                        JobBidId = jobBid?.Id ?? 0,
                        CompanyId = companyId,
                        Stage = MapToInspectionStage(rule.RequiredInspectionType!),
                        Status = "PENDING",
                        RequiredPermits = rule.RequiredPermitType,
                        ChecklistItems = $"[{{\"code\":\"{rule.CodeBook?.Code}\",\"section\":\"{rule.CodeSection}\",\"rule\":\"{rule.RuleName}\"}}]",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.InspectionStages.Add(inspection);
                    createdInspections.Add(inspection);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Generated {createdInspections.Count} inspections for job {jobId}");

                return createdInspections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inspections from scopes");
                return new List<InspectionStage>();
            }
        }

        /// <summary>
        /// Get all required permit types for a job based on scopes
        /// </summary>
        public async Task<List<string>> GetRequiredPermitTypesAsync(int jobId)
        {
            var jobScopes = await _context.JobScopes
                .Where(js => js.JobId == jobId)
                .Include(js => js.ScopeItem)
                .ToListAsync();

            var scopeCodes = jobScopes
                .Where(js => js.ScopeItem != null)
                .Select(js => js.ScopeItem!.ItemCode)
                .ToList();

            var permitTypes = await _context.CodeRules
                .Where(r => r.IsActive && scopeCodes.Contains(r.TriggerScope) && !string.IsNullOrEmpty(r.RequiredPermitType))
                .Select(r => r.RequiredPermitType!)
                .Distinct()
                .ToListAsync();

            return permitTypes;
        }

        /// <summary>
        /// Get filtered view for a specific department
        /// </summary>
        public async Task<object> GetDepartmentViewAsync(int jobId, string department)
        {
            var job = await _context.Jobs
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
                return new { error = "Job not found" };

            // Get scopes relevant to this department
            var jobScopes = await _context.JobScopes
                .Where(js => js.JobId == jobId)
                .Include(js => js.ScopeItem)
                .Where(js => js.ScopeItem != null && js.ScopeItem.Department == department)
                .ToListAsync();

            // Get code rules for this department
            var scopeCodes = jobScopes.Select(js => js.ScopeItem!.ItemCode).ToList();
            var codeRules = await _context.CodeRules
                .Where(r => r.IsActive && r.Department == department && scopeCodes.Contains(r.TriggerScope))
                .Include(r => r.CodeBook)
                .ToListAsync();

            // Get inspections for this department
            var jobBid = await _context.JobBids.FirstOrDefaultAsync(b => b.JobId == jobId);
            var inspections = jobBid != null
                ? await _context.InspectionStages.Where(i => i.JobBidId == jobBid.Id).ToListAsync()
                : new List<InspectionStage>();

            return new
            {
                department = department,
                jobId = jobId,
                jobName = job.Name,
                jobLocation = job.Location,
                scopes = jobScopes.Select(js => new
                {
                    itemCode = js.ScopeItem?.ItemCode,
                    itemName = js.ScopeItem?.ItemName,
                    status = js.Status,
                    notes = js.Notes
                }),
                codeReferences = codeRules.Select(r => new
                {
                    codeBook = r.CodeBook?.Code,
                    section = r.CodeSection,
                    rule = r.RuleName,
                    description = r.Description
                }),
                inspections = inspections.Select(i => new
                {
                    stage = i.Stage,
                    status = i.Status,
                    passed = i.Passed,
                    date = i.InspectionDate
                }),
                permitTypes = codeRules
                    .Where(r => !string.IsNullOrEmpty(r.RequiredPermitType))
                    .Select(r => r.RequiredPermitType)
                    .Distinct()
            };
        }

        /// <summary>
        /// Map rule inspection type to inspection stage
        /// </summary>
        private string MapToInspectionStage(string inspectionType)
        {
            return inspectionType.ToUpper() switch
            {
                var t when t.Contains("ROUGH") => "ROUGH",
                var t when t.Contains("FINAL") => "FINAL_SIGNOFF",
                var t when t.Contains("INSULATION") => "SECOND",
                _ => "ROUGH"
            };
        }

        /// <summary>
        /// Seed Massachusetts code books
        /// </summary>
        public async Task SeedMassachusettsCodesAsync()
        {
            if (await _context.CodeBooks.AnyAsync())
                return;

            var codeBooks = new List<CodeBook>
            {
                new CodeBook
                {
                    Code = "780 CMR",
                    Name = "Massachusetts State Building Code",
                    Edition = "10th Edition (2021 I-Codes)",
                    Jurisdiction = "Massachusetts",
                    AppliesToDepartment = DepartmentCodes.Building,
                    Description = "Based on 2021 IBC/IRC/IEBC/IECC with MA amendments",
                    ReferenceUrl = "https://www.mass.gov/780-cmr-state-building-code"
                },
                new CodeBook
                {
                    Code = "248 CMR",
                    Name = "Massachusetts Plumbing & Gas Code",
                    Edition = "Uniform State Plumbing Code 10.00",
                    Jurisdiction = "Massachusetts",
                    AppliesToDepartment = DepartmentCodes.Plumbing,
                    Description = "Plumbing and gas fitting requirements",
                    ReferenceUrl = "https://www.mass.gov/248-cmr"
                },
                new CodeBook
                {
                    Code = "527 CMR 12.00",
                    Name = "Massachusetts Electrical Code",
                    Edition = "Based on NEC",
                    Jurisdiction = "Massachusetts",
                    AppliesToDepartment = DepartmentCodes.Electrical,
                    Description = "Electrical installation requirements based on NFPA 70 NEC",
                    ReferenceUrl = "https://www.mass.gov/527-cmr"
                },
                new CodeBook
                {
                    Code = "527 CMR 1.00",
                    Name = "Massachusetts Fire Safety Code",
                    Edition = "Current",
                    Jurisdiction = "Massachusetts",
                    AppliesToDepartment = DepartmentCodes.Fire,
                    Description = "Fire alarm, sprinklers, hazardous uses, aligned with NFPA fire code",
                    ReferenceUrl = "https://www.mass.gov/527-cmr"
                },
                new CodeBook
                {
                    Code = "521 CMR",
                    Name = "Massachusetts Accessibility Code (MAAB)",
                    Edition = "Current + 2010 ADA Standards",
                    Jurisdiction = "Massachusetts",
                    AppliesToDepartment = DepartmentCodes.Accessibility,
                    Description = "Architectural Access Board accessibility requirements",
                    ReferenceUrl = "https://www.mass.gov/521-cmr"
                }
            };

            _context.CodeBooks.AddRange(codeBooks);
            await _context.SaveChangesAsync();

            // Add code rules
            await SeedCodeRulesAsync();

            _logger.LogInformation("Seeded Massachusetts code books");
        }

        private async Task SeedCodeRulesAsync()
        {
            var buildingCode = await _context.CodeBooks.FirstOrDefaultAsync(c => c.Code == "780 CMR");
            var plumbingCode = await _context.CodeBooks.FirstOrDefaultAsync(c => c.Code == "248 CMR");
            var electricalCode = await _context.CodeBooks.FirstOrDefaultAsync(c => c.Code == "527 CMR 12.00");
            var fireCode = await _context.CodeBooks.FirstOrDefaultAsync(c => c.Code == "527 CMR 1.00");

            if (buildingCode == null) return;

            var codeRules = new List<CodeRule>
            {
                // Building rules
                new CodeRule { CodeBookId = buildingCode.Id, RuleName = "Roofing Permit", TriggerScope = "ROOFING", RequiredPermitType = "BUILDING", RequiredInspectionType = "ROUGH_BUILDING", CodeSection = "R905", Department = DepartmentCodes.Building, InspectionOrder = 10 },
                new CodeRule { CodeBookId = buildingCode.Id, RuleName = "Siding Permit", TriggerScope = "SIDING", RequiredPermitType = "BUILDING", RequiredInspectionType = "ROUGH_BUILDING", CodeSection = "R703", Department = DepartmentCodes.Building, InspectionOrder = 10 },
                new CodeRule { CodeBookId = buildingCode.Id, RuleName = "Window/Door Permit", TriggerScope = "WINDOWS_DOORS", RequiredPermitType = "BUILDING", RequiredInspectionType = "ROUGH_BUILDING", CodeSection = "R612", Department = DepartmentCodes.Building, InspectionOrder = 10 },
                new CodeRule { CodeBookId = buildingCode.Id, RuleName = "Deck/Porch Permit", TriggerScope = "DECKS_PORCHES", RequiredPermitType = "BUILDING", RequiredInspectionType = "ROUGH_BUILDING", CodeSection = "R507", Department = DepartmentCodes.Building, InspectionOrder = 10 },
                new CodeRule { CodeBookId = buildingCode.Id, RuleName = "Structural Framing", TriggerScope = "FRAMING", RequiredPermitType = "BUILDING", RequiredInspectionType = "ROUGH_FRAMING", CodeSection = "R602", Department = DepartmentCodes.Building, InspectionOrder = 5 },
                new CodeRule { CodeBookId = buildingCode.Id, RuleName = "Load Bearing Wall", TriggerScope = "LOAD_BEARING_WALL", RequiredPermitType = "BUILDING", RequiredInspectionType = "ROUGH_FRAMING", CodeSection = "R602.3", Department = DepartmentCodes.Building, InspectionOrder = 5 },
                new CodeRule { CodeBookId = buildingCode.Id, RuleName = "New Bedroom Egress", TriggerScope = "NEW_BEDROOM", RequiredPermitType = "BUILDING", RequiredInspectionType = "FINAL_BUILDING", CodeSection = "R310", Department = DepartmentCodes.Building, InspectionOrder = 90 },
                new CodeRule { CodeBookId = buildingCode.Id, RuleName = "Stairs/Openings", TriggerScope = "STAIRS", RequiredPermitType = "BUILDING", RequiredInspectionType = "ROUGH_BUILDING", CodeSection = "R311", Department = DepartmentCodes.Building, InspectionOrder = 20 },
                new CodeRule { CodeBookId = buildingCode.Id, RuleName = "Insulation Inspection", TriggerScope = "INSULATION", RequiredPermitType = "BUILDING", RequiredInspectionType = "INSULATION", CodeSection = "N1102", Department = DepartmentCodes.Building, InspectionOrder = 50 },
            };

            // Plumbing rules
            if (plumbingCode != null)
            {
                codeRules.AddRange(new List<CodeRule>
                {
                    new CodeRule { CodeBookId = plumbingCode.Id, RuleName = "Water Supply Permit", TriggerScope = "WATER_SUPPLY", RequiredPermitType = "PLUMBING", RequiredInspectionType = "ROUGH_PLUMBING", CodeSection = "248 CMR 10.00", Department = DepartmentCodes.Plumbing, InspectionOrder = 15 },
                    new CodeRule { CodeBookId = plumbingCode.Id, RuleName = "New DWV Permit", TriggerScope = "SEWER_DWV", RequiredPermitType = "PLUMBING", RequiredInspectionType = "ROUGH_PLUMBING", CodeSection = "248 CMR 10.00", Department = DepartmentCodes.Plumbing, InspectionOrder = 15 },
                    new CodeRule { CodeBookId = plumbingCode.Id, RuleName = "New Bathroom Permit", TriggerScope = "NEW_BATHROOM", RequiredPermitType = "PLUMBING", RequiredInspectionType = "ROUGH_PLUMBING", CodeSection = "248 CMR 10.00", Department = DepartmentCodes.Plumbing, InspectionOrder = 15 },
                    new CodeRule { CodeBookId = plumbingCode.Id, RuleName = "Water Heater Permit", TriggerScope = "WATER_HEATER", RequiredPermitType = "PLUMBING", RequiredInspectionType = "FINAL_PLUMBING", CodeSection = "248 CMR 10.00", Department = DepartmentCodes.Plumbing, InspectionOrder = 80 },
                    new CodeRule { CodeBookId = plumbingCode.Id, RuleName = "Gas Piping Permit", TriggerScope = "GAS_PIPING", RequiredPermitType = "GAS", RequiredInspectionType = "ROUGH_GAS", CodeSection = "248 CMR 5.00", Department = DepartmentCodes.Plumbing, InspectionOrder = 15 },
                    new CodeRule { CodeBookId = plumbingCode.Id, RuleName = "Boiler Replacement", TriggerScope = "BOILER", RequiredPermitType = "GAS", RequiredInspectionType = "FINAL_GAS", CodeSection = "248 CMR 5.00", Department = DepartmentCodes.Plumbing, InspectionOrder = 80 },
                });
            }

            // Electrical rules
            if (electricalCode != null)
            {
                codeRules.AddRange(new List<CodeRule>
                {
                    new CodeRule { CodeBookId = electricalCode.Id, RuleName = "Service Upgrade Permit", TriggerScope = "SERVICE_UPGRADE", RequiredPermitType = "ELECTRICAL", RequiredInspectionType = "ROUGH_ELECTRICAL", CodeSection = "Article 230", Department = DepartmentCodes.Electrical, InspectionOrder = 15 },
                    new CodeRule { CodeBookId = electricalCode.Id, RuleName = "Full Rewire Permit", TriggerScope = "FULL_REWIRE", RequiredPermitType = "ELECTRICAL", RequiredInspectionType = "ROUGH_ELECTRICAL", CodeSection = "Article 210", Department = DepartmentCodes.Electrical, InspectionOrder = 15 },
                    new CodeRule { CodeBookId = electricalCode.Id, RuleName = "Partial Circuits Permit", TriggerScope = "PARTIAL_CIRCUITS", RequiredPermitType = "ELECTRICAL", RequiredInspectionType = "ROUGH_ELECTRICAL", CodeSection = "Article 210", Department = DepartmentCodes.Electrical, InspectionOrder = 15 },
                    new CodeRule { CodeBookId = electricalCode.Id, RuleName = "Smoke/CO System", TriggerScope = "SMOKE_CO", RequiredPermitType = "ELECTRICAL", RequiredInspectionType = "FINAL_ELECTRICAL", CodeSection = "Article 760", Department = DepartmentCodes.Electrical, InspectionOrder = 85 },
                });
            }

            // Fire rules
            if (fireCode != null)
            {
                codeRules.AddRange(new List<CodeRule>
                {
                    new CodeRule { CodeBookId = fireCode.Id, RuleName = "Fire Alarm System", TriggerScope = "FIRE_ALARM", RequiredPermitType = "FIRE", RequiredInspectionType = "FINAL_FIRE", CodeSection = "527 CMR 1.00", Department = DepartmentCodes.Fire, InspectionOrder = 85 },
                    new CodeRule { CodeBookId = fireCode.Id, RuleName = "Sprinkler System", TriggerScope = "SPRINKLER", RequiredPermitType = "FIRE", RequiredInspectionType = "ROUGH_FIRE", CodeSection = "527 CMR 1.00", Department = DepartmentCodes.Fire, InspectionOrder = 25 },
                });
            }

            _context.CodeRules.AddRange(codeRules);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Seed scope categories and items
        /// </summary>
        public async Task SeedScopeCategoriesAsync()
        {
            if (await _context.ScopeCategories.AnyAsync())
                return;

            var categories = new List<ScopeCategory>
            {
                new ScopeCategory { CategoryCode = "EXTERIOR", CategoryName = "Exterior Work", DisplayOrder = 1 },
                new ScopeCategory { CategoryCode = "STRUCTURAL", CategoryName = "Interior - Structural/Framing", DisplayOrder = 2 },
                new ScopeCategory { CategoryCode = "PLUMBING", CategoryName = "Plumbing Systems", DisplayOrder = 3 },
                new ScopeCategory { CategoryCode = "ELECTRICAL", CategoryName = "Electrical Systems", DisplayOrder = 4 },
                new ScopeCategory { CategoryCode = "HVAC", CategoryName = "HVAC / Mechanical", DisplayOrder = 5 },
                new ScopeCategory { CategoryCode = "SHEET_METAL", CategoryName = "Sheet Metal", DisplayOrder = 6 },
                new ScopeCategory { CategoryCode = "FINISHES", CategoryName = "Interior Finishes", DisplayOrder = 7 },
                new ScopeCategory { CategoryCode = "ACCESSIBILITY", CategoryName = "Accessibility / ADA / MAAB", DisplayOrder = 8 },
            };

            _context.ScopeCategories.AddRange(categories);
            await _context.SaveChangesAsync();

            // Add scope items
            var exterior = categories.First(c => c.CategoryCode == "EXTERIOR");
            var structural = categories.First(c => c.CategoryCode == "STRUCTURAL");
            var plumbing = categories.First(c => c.CategoryCode == "PLUMBING");
            var electrical = categories.First(c => c.CategoryCode == "ELECTRICAL");
            var hvac = categories.First(c => c.CategoryCode == "HVAC");
            var sheetMetal = categories.First(c => c.CategoryCode == "SHEET_METAL");
            var finishes = categories.First(c => c.CategoryCode == "FINISHES");
            var accessibility = categories.First(c => c.CategoryCode == "ACCESSIBILITY");

            var scopeItems = new List<ScopeItem>
            {
                // Exterior
                new ScopeItem { ScopeCategoryId = exterior.Id, ItemCode = "ROOFING", ItemName = "Roofing", TradeType = TradeTypes.Roofing, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = exterior.Id, ItemCode = "SIDING", ItemName = "Siding", TradeType = TradeTypes.Siding, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = exterior.Id, ItemCode = "WINDOWS_DOORS", ItemName = "Windows/Doors", TradeType = TradeTypes.Windows, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = exterior.Id, ItemCode = "DECKS_PORCHES", ItemName = "Decks/Porches", TradeType = TradeTypes.Framing, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = exterior.Id, ItemCode = "MASONRY", ItemName = "Masonry/Foundations", TradeType = TradeTypes.Masonry, Department = DepartmentCodes.Building },

                // Structural
                new ScopeItem { ScopeCategoryId = structural.Id, ItemCode = "DEMO", ItemName = "Demo (interior gut, partial)", TradeType = TradeTypes.Demo, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = structural.Id, ItemCode = "LOAD_BEARING_WALL", ItemName = "Load-bearing wall changes", TradeType = TradeTypes.Framing, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = structural.Id, ItemCode = "FRAMING", ItemName = "Floor framing / beams", TradeType = TradeTypes.Framing, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = structural.Id, ItemCode = "STAIRS", ItemName = "New stairs/openings", TradeType = TradeTypes.Framing, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = structural.Id, ItemCode = "INSULATION", ItemName = "Insulation", TradeType = TradeTypes.Framing, Department = DepartmentCodes.Building },

                // Plumbing
                new ScopeItem { ScopeCategoryId = plumbing.Id, ItemCode = "WATER_SUPPLY", ItemName = "Water Supply (new main/relocation)", TradeType = TradeTypes.Plumber, Department = DepartmentCodes.Plumbing, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = plumbing.Id, ItemCode = "WATER_HEATER", ItemName = "Water heater / indirect tank", TradeType = TradeTypes.Plumber, Department = DepartmentCodes.Plumbing, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = plumbing.Id, ItemCode = "SEWER_DWV", ItemName = "Sewer / DWV (new/altered)", TradeType = TradeTypes.Plumber, Department = DepartmentCodes.Plumbing, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = plumbing.Id, ItemCode = "NEW_BATHROOM", ItemName = "New bathroom or kitchen", TradeType = TradeTypes.Plumber, Department = DepartmentCodes.Plumbing, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = plumbing.Id, ItemCode = "BASEMENT_BATH", ItemName = "Basement bath / ejector", TradeType = TradeTypes.Plumber, Department = DepartmentCodes.Plumbing, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = plumbing.Id, ItemCode = "BOILER", ItemName = "Boiler replacement", TradeType = TradeTypes.Plumber, Department = DepartmentCodes.Plumbing, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = plumbing.Id, ItemCode = "GAS_PIPING", ItemName = "Gas piping & vents", TradeType = TradeTypes.GasFitter, Department = DepartmentCodes.Plumbing, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = plumbing.Id, ItemCode = "RADIANT_FLOOR", ItemName = "Radiant floor heat", TradeType = TradeTypes.Plumber, Department = DepartmentCodes.Plumbing, RequiresLicensedTrade = true },

                // Electrical
                new ScopeItem { ScopeCategoryId = electrical.Id, ItemCode = "SERVICE_UPGRADE", ItemName = "Service upgrade", TradeType = TradeTypes.Electrician, Department = DepartmentCodes.Electrical, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = electrical.Id, ItemCode = "FULL_REWIRE", ItemName = "Full rewire", TradeType = TradeTypes.Electrician, Department = DepartmentCodes.Electrical, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = electrical.Id, ItemCode = "PARTIAL_CIRCUITS", ItemName = "Partial circuits / lighting only", TradeType = TradeTypes.Electrician, Department = DepartmentCodes.Electrical, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = electrical.Id, ItemCode = "SMOKE_CO", ItemName = "Smoke/CO system", TradeType = TradeTypes.Electrician, Department = DepartmentCodes.Electrical, RequiresLicensedTrade = true },

                // HVAC
                new ScopeItem { ScopeCategoryId = hvac.Id, ItemCode = "MINI_SPLITS", ItemName = "Mini-splits / heat pumps", TradeType = TradeTypes.HVAC, Department = DepartmentCodes.Mechanical, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = hvac.Id, ItemCode = "FURNACE", ItemName = "Furnace/air handler", TradeType = TradeTypes.HVAC, Department = DepartmentCodes.Mechanical, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = hvac.Id, ItemCode = "CONDENSER", ItemName = "Condenser replacement", TradeType = TradeTypes.HVAC, Department = DepartmentCodes.Mechanical, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = hvac.Id, ItemCode = "VENTILATION", ItemName = "Ventilation (bath fans, range hoods)", TradeType = TradeTypes.HVAC, Department = DepartmentCodes.Mechanical },

                // Sheet Metal
                new ScopeItem { ScopeCategoryId = sheetMetal.Id, ItemCode = "DUCT_SYSTEMS", ItemName = "New duct systems", TradeType = TradeTypes.SheetMetal, Department = DepartmentCodes.Mechanical, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = sheetMetal.Id, ItemCode = "HOOD_DUCTWORK", ItemName = "Hood / specialty ductwork", TradeType = TradeTypes.SheetMetal, Department = DepartmentCodes.Mechanical, RequiresLicensedTrade = true },
                new ScopeItem { ScopeCategoryId = sheetMetal.Id, ItemCode = "CUSTOM_SHEET_METAL", ItemName = "Custom sheet metal", TradeType = TradeTypes.SheetMetal, Department = DepartmentCodes.Mechanical, RequiresLicensedTrade = true },

                // Finishes
                new ScopeItem { ScopeCategoryId = finishes.Id, ItemCode = "DRYWALL", ItemName = "Drywall / Plaster", TradeType = TradeTypes.Drywall, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = finishes.Id, ItemCode = "FLOORING", ItemName = "Flooring (tile, hardwood, LVP, carpet)", TradeType = TradeTypes.Flooring, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = finishes.Id, ItemCode = "TILE_SHOWER", ItemName = "Tile showers / tub surrounds", TradeType = TradeTypes.Tile, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = finishes.Id, ItemCode = "CABINETS", ItemName = "Kitchen cabinets", TradeType = TradeTypes.Cabinets, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = finishes.Id, ItemCode = "VANITIES", ItemName = "Vanities / countertops", TradeType = TradeTypes.FinishCarpentry, Department = DepartmentCodes.Building },
                new ScopeItem { ScopeCategoryId = finishes.Id, ItemCode = "PAINT", ItemName = "Paint", TradeType = TradeTypes.Painting, Department = DepartmentCodes.Building },

                // Accessibility
                new ScopeItem { ScopeCategoryId = accessibility.Id, ItemCode = "COMMERCIAL_SPACE", ItemName = "Commercial space", Department = DepartmentCodes.Accessibility },
                new ScopeItem { ScopeCategoryId = accessibility.Id, ItemCode = "PUBLIC_ENTRY", ItemName = "Public entry", Department = DepartmentCodes.Accessibility },
                new ScopeItem { ScopeCategoryId = accessibility.Id, ItemCode = "PUBLIC_TOILET", ItemName = "Public toilet rooms", Department = DepartmentCodes.Accessibility },
                new ScopeItem { ScopeCategoryId = accessibility.Id, ItemCode = "RAMPS_LIFTS", ItemName = "Ramps / lifts / elevators", Department = DepartmentCodes.Accessibility },
            };

            _context.ScopeItems.AddRange(scopeItems);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded scope categories and items");
        }
    }
}
