using System;
using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    /// <summary>
    /// Junction table allowing a single Sub-Contractor to work for multiple companies
    /// Each company has their own isolation, but subs can be hired by multiple companies
    /// </summary>
    public class SubContractorCompany
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubContractorUserId { get; set; }
        public User SubContractorUser { get; set; } = null!;

        [Required]
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // Status: ACTIVE, INACTIVE, SUSPENDED, REMOVED
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "ACTIVE";

        // When the sub-contractor was added to this company
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Verification/approval by company admin
        public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }
        public int? VerifiedByUserId { get; set; }

        // Contact info specific to this company relationship
        public string? CompanyContactName { get; set; }
        public string? CompanyContactPhone { get; set; }
        public string? CompanyContactEmail { get; set; }

        // Specializations for this company
        public string? Specializations { get; set; } // JSON: ["Electrical", "HVAC", "Plumbing"]

        // Billing info
        public string? BillingRate { get; set; }
        public string? PaymentTerms { get; set; }

        // Stats
        public int TotalJobsBid { get; set; } = 0;
        public int TotalJobsAccepted { get; set; } = 0;
        public int TotalJobsCompleted { get; set; } = 0;
        public double SuccessRate { get; set; } = 0.0; // Percentage

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Request to invite a sub-contractor to work for a company
    /// </summary>
    public class AddSubContractorRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        public string? Specializations { get; set; }
        public string? BillingRate { get; set; }
        public string? PaymentTerms { get; set; }
    }

    /// <summary>
    /// Sub-contractor profile visible to multiple companies
    /// </summary>
    public class SubContractorProfile
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Specializations { get; set; } = string.Empty;
        public double AverageSuccessRate { get; set; }
        public int TotalJobsCompleted { get; set; }
        public List<string> CompaniesWorkedWith { get; set; } = new();
    }
}
