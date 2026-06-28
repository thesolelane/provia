using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Services
{
    public interface IUserDeactivationService
    {
        Task<bool> DeactivateUserAsync(int userId, int deactivatedByUserId, string? reason = null, string? notes = null);
        Task<DeactivatedUser?> GetDeactivatedUserAsync(int originalUserId);

        Task<List<DeactivatedUser>> GetDeactivatedUsersAsync(int companyId);
    }

    public class UserDeactivationService : IUserDeactivationService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<UserDeactivationService> _logger;

        public UserDeactivationService(JobTrackerContext context, ILogger<UserDeactivationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> DeactivateUserAsync(int userId, int deactivatedByUserId, string? reason = null, string? notes = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Find the user to deactivate
                var user = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found or already inactive", userId);
                    return false;
                }

                // Find the user performing the deactivation
                var deactivatedBy = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == deactivatedByUserId && u.IsActive);

                if (deactivatedBy == null)
                {
                    _logger.LogWarning("Deactivating user with ID {DeactivatedByUserId} not found", deactivatedByUserId);
                    return false;
                }

                // Generate shortened user ID for archive reference
                var shortenedId = $"DEL-{user.Id.ToString().Substring(Math.Max(0, user.Id.ToString().Length - 4)).PadLeft(4, '0')}";

                // Create the deactivated user record (NO PASSWORDS STORED)
                var deactivatedUser = new DeactivatedUser
                {
                    OriginalUserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    UserCode = user.UserCode,
                    LanguagePreference = user.LanguagePreference,
                    OriginalCreatedAt = user.CreatedAt,
                    OriginalUpdatedAt = user.UpdatedAt,
                    DeactivatedAt = DateTime.UtcNow,
                    OriginalCreatedByUserId = user.CreatedByUserId,
                    DeactivatedByUserId = deactivatedByUserId,
                    CompanyId = user.CompanyId,
                    
                    // Store original username for reference only (no passwords)
                    OriginalUsername = user.Username,
                    
                    // Generate shortened ID for archive reference
                    ShortenedUserId = shortenedId,
                    
                    // Archive verification and status information
                    WasPhoneVerified = user.IsPhoneVerified,
                    WasEmailVerified = user.IsEmailVerified,
                    LastLoginAt = user.LastLoginAt,
                    HadLocationTrackingConsent = user.LocationTrackingConsent,
                    
                    // Deactivation details
                    DeactivationReason = reason,
                    DeactivationNotes = notes,
                    CanBeReactivated = false // Account cannot be reused but remains searchable in archive
                };

                // Add the deactivated user record
                _context.DeactivatedUsers.Add(deactivatedUser);

                // Mark the original user as inactive and clear ALL sensitive data
                user.IsActive = false;
                user.PasswordHash = null;
                user.PinHash = null;
                user.Username = shortenedId; // Set username to shortened ID for archive reference
                user.Email = $"deleted_{shortenedId}@archive.local"; // Modify email to prevent conflicts
                user.PhoneNumber = null; // Clear phone number
                user.PhoneVerificationCode = null;
                user.EmailVerificationCode = null;
                user.IsPhoneVerified = false;
                user.IsEmailVerified = false;
                user.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("User {UserId} ({UserName}) successfully deactivated by user {DeactivatedByUserId}. Credentials archived to DeactivatedUser ID {DeactivatedUserId}", 
                    userId, user.GetDisplayName(), deactivatedByUserId, deactivatedUser.Id);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deactivating user {UserId}", userId);
                return false;
            }
        }

        public async Task<DeactivatedUser?> GetDeactivatedUserAsync(int originalUserId)
        {
            return await _context.DeactivatedUsers
                .Include(du => du.DeactivatedBy)
                .Include(du => du.Company)
                .FirstOrDefaultAsync(du => du.OriginalUserId == originalUserId);
        }

        public async Task<List<DeactivatedUser>> GetDeactivatedUsersAsync(int companyId)
        {
            return await _context.DeactivatedUsers
                .Include(du => du.DeactivatedBy)
                .Where(du => du.CompanyId == companyId)
                .OrderByDescending(du => du.DeactivatedAt)
                .ToListAsync();
        }
    }
}