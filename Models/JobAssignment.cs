using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTrackerApp.Models
{
    public class JobAssignment
    {
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }
        
        [ForeignKey("JobId")]
        public Job? Job { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        public DateTime AssignmentDate { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [StringLength(100)]
        public string Role { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
