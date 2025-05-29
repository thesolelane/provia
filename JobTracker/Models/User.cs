using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.RegularUser;

        [Required]
        [StringLength(10)]
        public string LanguagePreference { get; set; } = "en"; // en, es, fr, etc.

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedByUserId { get; set; }
        public User? CreatedBy { get; set; }

        // Company association for multi-tenant support
        [Required]
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // For password hash storage (simplified for demo)
        public string? PasswordHash { get; set; }

        // For phone verification
        public string? PhoneVerificationCode { get; set; }
        public DateTime? PhoneVerificationExpiry { get; set; }
        public bool IsPhoneVerified { get; set; } = false;

        // For email verification
        public string? EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationExpiry { get; set; }
        public bool IsEmailVerified { get; set; } = false;

        // For tracking login activity
        public DateTime? LastLoginAt { get; set; }

        // For GPS tracking consent during work hours
        public bool LocationTrackingConsent { get; set; } = false;

        public string GetDisplayName() => $"{FirstName} {LastName}";
        public string GetRoleDisplayName() => Role switch
        {
            UserRole.MasterAdmin => "Master Admin",
            UserRole.Admin => "Admin",
            UserRole.RegularUser => "User",
            _ => "Unknown"
        };
    }

    public enum UserRole
    {
        RegularUser = 0,
        Admin = 1,
        MasterAdmin = 2
    }

    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Password { get; set; } = string.Empty;
        public string LanguagePreference { get; set; } = "en";
    }

    public class RegisterRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string LanguagePreference { get; set; } = "en";

        [Required]
        public UserRole RequestedRole { get; set; } = UserRole.RegularUser;
    }

    public class UpdateProfileRequest
    {
        public string? PhoneNumber { get; set; }
        public bool LocationTrackingConsent { get; set; }
        public string? LanguagePreference { get; set; }
    }
}