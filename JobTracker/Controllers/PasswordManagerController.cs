using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using System.Security.Cryptography;
using System.Text;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordManagerController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordManagerController> _logger;

        public PasswordManagerController(JobTrackerContext context, IEmailService emailService, ILogger<PasswordManagerController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("send-verification/{userId}")]
        public async Task<IActionResult> SendVerificationEmail(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (user.IsEmailVerified)
                {
                    return BadRequest(new { message = "User email is already verified" });
                }

                // Generate verification code and temporary password
                var verificationCode = GenerateVerificationCode();
                var temporaryPassword = GenerateTemporaryPassword();

                // Update user with verification details
                user.EmailVerificationCode = verificationCode;
                user.EmailVerificationExpiry = DateTime.UtcNow.AddHours(24); // 24 hour expiry
                user.PasswordHash = HashPassword(temporaryPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send verification email
                var emailSent = await _emailService.SendVerificationEmailAsync(
                    user.Email!,
                    user.FirstName,
                    verificationCode,
                    temporaryPassword
                );

                if (emailSent)
                {
                    return Ok(new
                    {
                        message = "Verification email sent successfully",
                        email = user.Email,
                        expiresAt = user.EmailVerificationExpiry
                    });
                }
                else
                {
                    return Ok(new
                    {
                        message = "Verification code generated (email service not configured)",
                        verificationCode,
                        temporaryPassword,
                        email = user.Email,
                        note = "Please provide email service credentials to send actual emails"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email for user {UserId}", userId);
                return StatusCode(500, new { message = "Failed to send verification email" });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null)
                {
                    return BadRequest(new { message = "Invalid email address" });
                }

                if (user.IsEmailVerified)
                {
                    return BadRequest(new { message = "Email is already verified" });
                }

                if (user.EmailVerificationCode != request.VerificationCode)
                {
                    return BadRequest(new { message = "Invalid verification code" });
                }

                if (user.EmailVerificationExpiry < DateTime.UtcNow)
                {
                    return BadRequest(new { message = "Verification code has expired" });
                }

                // Verify email and clear verification data
                user.IsEmailVerified = true;
                user.EmailVerificationCode = null;
                user.EmailVerificationExpiry = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send welcome email
                await _emailService.SendWelcomeEmailAsync(
                    user.Email!,
                    user.FirstName,
                    user.Company?.CompanyName ?? "Your Company"
                );

                return Ok(new
                {
                    message = "Email verified successfully",
                    mustChangePassword = true,
                    loginUrl = "/login"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for {Email}", request.Email);
                return StatusCode(500, new { message = "Email verification failed" });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null)
                {
                    return BadRequest(new { message = "Invalid email address" });
                }

                // Verify current password
                if (user.PasswordHash != HashPassword(request.CurrentPassword))
                {
                    return BadRequest(new { message = "Current password is incorrect" });
                }

                // Validate new password strength
                if (!IsPasswordStrong(request.NewPassword))
                {
                    return BadRequest(new { message = "Password must be at least 8 characters with uppercase, lowercase, number, and special character" });
                }

                // Update password
                user.PasswordHash = HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for {Email}", request.Email);
                return StatusCode(500, new { message = "Password change failed" });
            }
        }

        [HttpPost("reset-password-request")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null)
                {
                    // Don't reveal if email exists for security
                    return Ok(new { message = "If this email exists, a reset link has been sent" });
                }

                // Generate reset code
                var resetCode = GenerateResetCode();
                
                user.EmailVerificationCode = resetCode; // Reuse field for reset
                user.EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(30); // 30 minute expiry
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send reset email
                var emailSent = await _emailService.SendPasswordResetEmailAsync(
                    user.Email!,
                    user.FirstName,
                    resetCode
                );

                if (!emailSent)
                {
                    return Ok(new
                    {
                        message = "Reset code generated (email service not configured)",
                        resetCode,
                        email = user.Email,
                        note = "Please provide email service credentials to send actual emails"
                    });
                }

                return Ok(new { message = "If this email exists, a reset link has been sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset for {Email}", request.Email);
                return StatusCode(500, new { message = "Password reset request failed" });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetCompleteRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user == null)
                {
                    return BadRequest(new { message = "Invalid reset request" });
                }

                if (user.EmailVerificationCode != request.ResetCode)
                {
                    return BadRequest(new { message = "Invalid reset code" });
                }

                if (user.EmailVerificationExpiry < DateTime.UtcNow)
                {
                    return BadRequest(new { message = "Reset code has expired" });
                }

                // Validate new password strength
                if (!IsPasswordStrong(request.NewPassword))
                {
                    return BadRequest(new { message = "Password must be at least 8 characters with uppercase, lowercase, number, and special character" });
                }

                // Update password and clear reset data
                user.PasswordHash = HashPassword(request.NewPassword);
                user.EmailVerificationCode = null;
                user.EmailVerificationExpiry = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for {Email}", request.Email);
                return StatusCode(500, new { message = "Password reset failed" });
            }
        }

        [HttpPost("admin-reset-password/{userId}")]
        public async Task<IActionResult> AdminResetPassword(int userId, [FromBody] AdminPasswordResetRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Generate new temporary password
                var newTempPassword = GenerateTemporaryPassword();
                
                user.PasswordHash = HashPassword(newTempPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Password reset successfully",
                    temporaryPassword = newTempPassword,
                    email = user.Email,
                    note = "User must change password on next login"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in admin password reset for user {UserId}", userId);
                return StatusCode(500, new { message = "Admin password reset failed" });
            }
        }

        [HttpPost("block-user/{userId}")]
        public async Task<IActionResult> BlockUser(int userId, [FromBody] BlockUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "User account blocked successfully",
                    reason = request.Reason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking user {UserId}", userId);
                return StatusCode(500, new { message = "Failed to block user" });
            }
        }

        private static string GenerateVerificationCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return Math.Abs(BitConverter.ToInt32(bytes, 0)).ToString("D8")[..6];
        }

        private static string GenerateResetCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return Math.Abs(BitConverter.ToInt32(bytes, 0)).ToString("D8")[..8];
        }

        private static string GenerateTemporaryPassword()
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
            using var rng = RandomNumberGenerator.Create();
            var result = new char[12];
            var bytes = new byte[4];

            for (int i = 0; i < 12; i++)
            {
                rng.GetBytes(bytes);
                result[i] = chars[Math.Abs(BitConverter.ToInt32(bytes, 0)) % chars.Length];
            }

            return new string(result);
        }

        private static bool IsPasswordStrong(string password)
        {
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            if (!password.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch))) return false;
            return true;
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "PROVIA_Password_Salt"));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public class EmailVerificationRequest
    {
        public string Email { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class PasswordResetRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class PasswordResetCompleteRequest
    {
        public string Email { get; set; } = string.Empty;
        public string ResetCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class AdminPasswordResetRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class BlockUserRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}