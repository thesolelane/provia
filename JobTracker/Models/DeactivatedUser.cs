using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class DeactivatedUser
    {
        public int Id { get; set; }

        [Required]
        public int OriginalUserId { get; set; }

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
        public int Role { get; set; }

        [StringLength(50)]
        public string? UserCode { get; set; }

        [Required]
        [StringLength(10)]
        public string LanguagePreference { get; set; } = "en";

        public DateTime OriginalCreatedAt { get; set; }
        public DateTime OriginalUpdatedAt { get; set; }
        public DateTime DeactivatedAt { get; set; } = DateTime.UtcNow;

        public int? OriginalCreatedByUserId { get; set; }
        public int DeactivatedByUserId { get; set; }
        public User DeactivatedBy { get; set; } = null!;

        [Required]
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // Note: No passwords or credentials are stored for security
        // Original username for reference only
        public string? OriginalUsername { get; set; }

        // Verification status at time of deactivation
        public bool WasPhoneVerified { get; set; }
        public bool WasEmailVerified { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool HadLocationTrackingConsent { get; set; }

        // Deactivation reason and notes
        [StringLength(500)]
        public string? DeactivationReason { get; set; }

        [StringLength(1000)]
        public string? DeactivationNotes { get; set; }

        // Shortened user ID for archive reference (account cannot be reused)
        public string? ShortenedUserId { get; set; }
        
        // Account is permanently deactivated but searchable in archive
        public bool CanBeReactivated { get; set; } = false;

        public string GetDisplayName() => $"{FirstName} {LastName}";
        public string GetRoleDisplayName() => Role switch
        {
            1510 => "Master Admin",
            1520 => "Admin", 
            2001 => "Field Operator",
            _ => "Unknown"
        };
    }
}