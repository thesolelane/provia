using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class UserJobAssignment
    {
        [Key]
        public int Id { get; set; }
        
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        [ForeignKey("Job")]
        public int JobId { get; set; }
        public Job Job { get; set; } = null!;
        
        public AssignmentRole Role { get; set; } = AssignmentRole.Worker;
        
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum AssignmentRole
    {
        Worker = 0,
        Supervisor = 1,
        ProjectManager = 2,
        Subcontractor = 3
    }
    
    public class Inspection
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string InspectionType { get; set; } = string.Empty; // "Framing", "Electrical", "Plumbing", etc.
        
        [ForeignKey("JobSection")]
        public int JobSectionId { get; set; }
        public JobSection JobSection { get; set; } = null!;
        
        public InspectionStatus Status { get; set; } = InspectionStatus.Scheduled;
        
        public DateTime ScheduledDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        
        [StringLength(100)]
        public string? InspectorName { get; set; }
        
        [StringLength(50)]
        public string? InspectorContact { get; set; }
        
        [StringLength(2000)]
        public string? Notes { get; set; }
        
        [StringLength(1000)]
        public string? FailureReasons { get; set; }
        
        public DateTime? ReinspectionDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum InspectionStatus
    {
        Scheduled = 0,
        InProgress = 1,
        Passed = 2,
        Failed = 3,
        Cancelled = 4,
        Rescheduled = 5
    }
}