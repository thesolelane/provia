using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTrackerApp.Models
{
    public class Job
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [StringLength(50)]
        public string JobNumber { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? TargetCompletionDate { get; set; }

        public DateTime? ActualCompletionDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        public string ClientName { get; set; } = string.Empty;

        [StringLength(100)]
        public string ClientEmail { get; set; } = string.Empty;

        [StringLength(20)]
        public string ClientPhone { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCost { get; set; }

        public int? ProjectManagerId { get; set; }
        [ForeignKey("ProjectManagerId")]
        public Employee? ProjectManager { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<JobSection> Sections { get; set; } = new List<JobSection>();
        public ICollection<JobAssignment> Assignments { get; set; } = new List<JobAssignment>();
    }
}
