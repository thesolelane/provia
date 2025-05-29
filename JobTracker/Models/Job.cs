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
        public string JobName { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(300)]
        public string Location { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string City { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string State { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string ZipCode { get; set; } = string.Empty;
        
        public JobStatus Status { get; set; } = JobStatus.Planning;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedCost { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCost { get; set; }
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        
        // GPS coordinates for geo-fencing
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        [ForeignKey("Company")]
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
        
        [ForeignKey("ProjectManager")]
        public int? ProjectManagerId { get; set; }
        public User? ProjectManager { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<JobSection> Sections { get; set; } = new List<JobSection>();
        public ICollection<MaterialRequest> MaterialRequests { get; set; } = new List<MaterialRequest>();
        public ICollection<ChangeRequest> ChangeRequests { get; set; } = new List<ChangeRequest>();
        public ICollection<UserJobAssignment> UserAssignments { get; set; } = new List<UserJobAssignment>();
    }
    
    public enum JobStatus
    {
        Planning = 0,
        PermitsPending = 1,
        InProgress = 2,
        InspectionPending = 3,
        OnHold = 4,
        Completed = 5,
        Cancelled = 6
    }
    

}