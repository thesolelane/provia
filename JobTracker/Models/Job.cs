using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string JobNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(300)]
        public string Location { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ClientName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string ClientEmail { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string ClientPhone { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Planning";
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCost { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime? TargetCompletionDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        
        public string? Notes { get; set; }
        
        // GPS coordinates for geo-fencing
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        public int? CompanyId { get; set; }
        
        // Import tracking
        public bool IsImported { get; set; } = false;
        public string? ImportedFrom { get; set; } // "Wave", "QuickBooks", etc.
        public DateTime? ImportedDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Company? Company { get; set; }
        public ICollection<JobSection> Sections { get; set; } = new List<JobSection>();
    }
    
    public enum JobStatus
    {
        PendingApproval = -1,  // Jobs imported and waiting for admin approval
        Planning = 0,
        PermitsPending = 1,
        InProgress = 2,
        InspectionPending = 3,
        OnHold = 4,
        Completed = 5,
        Cancelled = 6
    }
    

}