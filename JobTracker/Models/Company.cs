using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string AccountNumber { get; set; } = string.Empty; // Unique company identifier

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(200)]
        public string ContactEmail { get; set; } = string.Empty;

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(10)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? ZipCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; } = "United States";

        public SubscriptionType SubscriptionType { get; set; } = SubscriptionType.Trial;
        
        public DateTime SubscriptionStartDate { get; set; } = DateTime.UtcNow;
        public DateTime? SubscriptionEndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsTrialExpired => SubscriptionType == SubscriptionType.Trial && 
                                     SubscriptionEndDate.HasValue && 
                                     SubscriptionEndDate.Value < DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Job> Jobs { get; set; } = new List<Job>();

        // License limits
        public int MaxUsers { get; set; } = 5; // Default for trial
        public int MaxJobs { get; set; } = 10; // Default for trial
        public bool CanUseAdvancedFeatures { get; set; } = false;

        public string GetDisplayName() => $"{CompanyName} ({AccountNumber})";
    }

    public enum SubscriptionType
    {
        Trial = 0,
        Basic = 1,
        Professional = 2,
        Enterprise = 3
    }

    public class CompanyRegistrationRequest
    {
        [Required]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string ContactEmail { get; set; } = string.Empty;

        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }

        [Required]
        public string AdminFirstName { get; set; } = string.Empty;

        [Required]
        public string AdminLastName { get; set; } = string.Empty;

        [Required]
        public string AdminPassword { get; set; } = string.Empty;

        public string LanguagePreference { get; set; } = "en";
        
        public SubscriptionType RequestedSubscription { get; set; } = SubscriptionType.Trial;
    }
}