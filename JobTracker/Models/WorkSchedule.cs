using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class WorkSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int JobId { get; set; }
        
        [Required]
        public DateTime ScheduledDate { get; set; }
        
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        
        [Required]
        public WorkScheduleStatus Status { get; set; }
        
        public string Notes { get; set; } = string.Empty;
        public string ApprovalNotes { get; set; } = string.Empty;
        
        public int? CreatedByUserId { get; set; }
        public int? ApprovedByUserId { get; set; }
        
        public DateTime? SubmittedForApprovalAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        [Required]
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        
        [ForeignKey("JobId")]
        public virtual Job Job { get; set; }
        
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedBy { get; set; }
        
        [ForeignKey("ApprovedByUserId")]
        public virtual User ApprovedBy { get; set; }
    }

    public enum WorkScheduleStatus
    {
        Draft = 0,
        Planned = 1,
        PendingApproval = 2,
        Approved = 3,
        Rejected = 4,
        InProgress = 5,
        Completed = 6
    }
}