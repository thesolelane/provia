using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class WorkTask
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public TaskStatus Status { get; set; } = TaskStatus.NotStarted;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        
        [ForeignKey("JobSection")]
        public int JobSectionId { get; set; }
        public JobSection JobSection { get; set; } = null!;
        
        [ForeignKey("AssignedUser")]
        public int? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }
        
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal EstimatedHours { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal ActualHours { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum TaskStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        OnHold = 3,
        Cancelled = 4
    }
    
    public enum TaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }
    
    public class MaterialRequest
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string ItemName { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(100)]
        public string? SKU { get; set; }
        
        public int Quantity { get; set; }
        
        [StringLength(50)]
        public string? Unit { get; set; } // e.g., "pieces", "feet", "gallons"
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? EstimatedCost { get; set; }
        
        [Required]
        [StringLength(100)]
        public string PreferredStore { get; set; } = string.Empty; // "HomeDepot", "Lowes", etc.
        
        [StringLength(200)]
        public string? StoreProductUrl { get; set; }
        
        public MaterialRequestStatus Status { get; set; } = MaterialRequestStatus.Pending;
        
        [ForeignKey("Job")]
        public int JobId { get; set; }
        public Job Job { get; set; } = null!;
        
        [ForeignKey("RequestedBy")]
        public int RequestedById { get; set; }
        public User RequestedBy { get; set; } = null!;
        
        [ForeignKey("ApprovedBy")]
        public int? ApprovedById { get; set; }
        public User? ApprovedBy { get; set; }
        
        public DateTime? NeededByDate { get; set; }
        public DateTime? OrderedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum MaterialRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Ordered = 2,
        Delivered = 3,
        Rejected = 4,
        Cancelled = 5
    }
    
    public class ChangeRequest
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Justification { get; set; }
        
        public ChangeRequestType Type { get; set; } = ChangeRequestType.ScopeChange;
        public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;
        
        [ForeignKey("Job")]
        public int JobId { get; set; }
        public Job Job { get; set; } = null!;
        
        [ForeignKey("RequestedBy")]
        public int RequestedById { get; set; }
        public User RequestedBy { get; set; } = null!;
        
        [ForeignKey("ReviewedBy")]
        public int? ReviewedById { get; set; }
        public User? ReviewedBy { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? EstimatedCostImpact { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? EstimatedTimeImpact { get; set; } // in days
        
        [StringLength(1000)]
        public string? ReviewNotes { get; set; }
        
        public DateTime? ReviewedDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum ChangeRequestType
    {
        ScopeChange = 0,
        MaterialChange = 1,
        DesignChange = 2,
        SafetyConcern = 3,
        CodeCompliance = 4,
        UnforeseenCondition = 5,
        ClientRequest = 6
    }
    
    public enum ChangeRequestStatus
    {
        Pending = 0,
        UnderReview = 1,
        Approved = 2,
        Rejected = 3,
        MoreInfoNeeded = 4,
        Implemented = 5
    }
}