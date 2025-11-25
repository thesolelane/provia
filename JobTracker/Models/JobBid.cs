using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    /// <summary>
    /// Job bid from sub-contractor
    /// Sub selects payment method at acceptance
    /// Gets advance % upon acceptance, rest after inspection
    /// </summary>
    public class JobBid
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }
        public Job? Job { get; set; }

        [Required]
        public int SubContractorUserId { get; set; }
        public User? SubContractor { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        // Bid Status: PENDING, ACCEPTED, REJECTED, WITHDRAWN, COMPLETED
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "PENDING";

        // Bid amount (what sub is bidding to do the work)
        [Column(TypeName = "decimal(18,2)")]
        public decimal BidAmount { get; set; }

        // Bid description/notes
        [StringLength(500)]
        public string? BidDescription { get; set; }

        // When accepted by foreman
        public DateTime? AcceptedAt { get; set; }
        public int? AcceptedByUserId { get; set; }

        // Payment method selected by sub at acceptance
        [StringLength(50)]
        public string? PaymentMethod { get; set; } // "DIRECT_DEPOSIT", "CHECK", "ACH", etc.

        // Advance payment tracking
        public decimal AdvancePercentage { get; set; } = 0m; // X% paid upfront
        public decimal AdvanceAmount { get; set; } = 0m; // Actual amount paid in advance
        public DateTime? AdvancePaidAt { get; set; }

        // Final payment tracking
        public decimal RemainingAmount { get; set; } = 0m; // Rest after inspection
        public DateTime? FinalPaidAt { get; set; }
        public bool IsInspectionPassed { get; set; } = false;

        // Work completion & supervisor approval
        public DateTime? WorkStartedAt { get; set; }
        public DateTime? WorkCompletedAt { get; set; }
        public bool SupervisorApproved { get; set; } = false;
        public DateTime? SupervisorApprovedAt { get; set; }
        public int? SupervisorApprovedByUserId { get; set; }
        public string? SupervisorNotes { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for job bid operations
    /// </summary>
    public class JobBidDto
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public int SubContractorUserId { get; set; }
        public string SubContractorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal BidAmount { get; set; }
        public string? BidDescription { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal AdvancePercentage { get; set; }
        public decimal AdvanceAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool SupervisorApproved { get; set; }
        public bool IsInspectionPassed { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request to place a bid on a job
    /// </summary>
    public class PlaceBidRequest
    {
        [Required]
        public int JobId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal BidAmount { get; set; }

        [StringLength(500)]
        public string? BidDescription { get; set; }
    }

    /// <summary>
    /// Request to accept a bid (with payment method selection)
    /// </summary>
    public class AcceptBidRequest
    {
        [Required]
        public int BidId { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [Range(0, 100)]
        public decimal AdvancePercentage { get; set; } = 50m; // Default 50% advance
    }

    /// <summary>
    /// Request for supervisor to approve completed work
    /// </summary>
    public class ApproveWorkRequest
    {
        [Required]
        public int BidId { get; set; }

        [StringLength(500)]
        public string? ApprovalNotes { get; set; }

        public bool InspectionPassed { get; set; } = false;
    }
}
