using JobTrackerApp.Data;
using JobTrackerApp.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTrackerApp.Services.BuildingCode
{
    public class BuildingCodeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BuildingCodeService> _logger;

        public BuildingCodeService(
            ApplicationDbContext context,
            ILogger<BuildingCodeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Models.BuildingCode>> GetAllCodes()
        {
            try
            {
                return await _context.BuildingCodes
                    .Where(bc => bc.IsActive)
                    .OrderBy(bc => bc.CodeNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all building codes");
                throw;
            }
        }

        public async Task<IEnumerable<Models.BuildingCode>> GetCodesBySection(SectionType sectionType)
        {
            try
            {
                return await _context.BuildingCodes
                    .Where(bc => bc.RelatedSectionType == sectionType && bc.IsActive)
                    .OrderBy(bc => bc.CodeNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving building codes for section type {SectionType}", sectionType);
                throw;
            }
        }

        public async Task<Models.BuildingCode?> GetCodeById(int id)
        {
            try
            {
                return await _context.BuildingCodes.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving building code with ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Models.BuildingCode>> SearchCodes(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllCodes();
                }

                searchTerm = searchTerm.ToLower();

                return await _context.BuildingCodes
                    .Where(bc => bc.IsActive &&
                               (bc.CodeNumber.ToLower().Contains(searchTerm) ||
                                bc.Title.ToLower().Contains(searchTerm) ||
                                bc.Description.ToLower().Contains(searchTerm) ||
                                bc.Category.ToLower().Contains(searchTerm)))
                    .OrderBy(bc => bc.CodeNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching building codes with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<Models.BuildingCode> AddCode(Models.BuildingCode code)
        {
            try
            {
                _context.BuildingCodes.Add(code);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added new building code {CodeNumber}: {Title}", code.CodeNumber, code.Title);
                return code;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding building code {CodeNumber}", code.CodeNumber);
                throw;
            }
        }

        public async Task<bool> UpdateCode(Models.BuildingCode code)
        {
            try
            {
                _context.Entry(code).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated building code {CodeNumber}: {Title}", code.CodeNumber, code.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating building code {CodeNumber}", code.CodeNumber);
                throw;
            }
        }

        public async Task<bool> DeleteCode(int id)
        {
            try
            {
                var code = await _context.BuildingCodes.FindAsync(id);
                if (code == null)
                {
                    return false;
                }

                // Soft delete
                code.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft-deleted building code {CodeNumber}: {Title}", code.CodeNumber, code.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting building code with ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Models.BuildingCode>> GetCodesByCategory(string category)
        {
            try
            {
                return await _context.BuildingCodes
                    .Where(bc => bc.Category.ToLower() == category.ToLower() && bc.IsActive)
                    .OrderBy(bc => bc.CodeNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving building codes for category {Category}", category);
                throw;
            }
        }
    }
}
