using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTrackerApp.Models
{
    public class TimeEntry
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        public int? JobId { get; set; }
        
        [ForeignKey("JobId")]
        public Job? Job { get; set; }

        [Required]
        public DateTime ClockInTime { get; set; }

        public DateTime? ClockOutTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalHours { get; set; }

        public bool IsManualEntry { get; set; } = false;

        [StringLength(200)]
        public string? Notes { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Open";

        public string? Location { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;

        // Method to calculate total hours
        public void CalculateTotalHours()
        {
            if (ClockOutTime.HasValue)
            {
                TimeSpan duration = ClockOutTime.Value - ClockInTime;
                TotalHours = (decimal)duration.TotalHours;
            }
            else
            {
                TotalHours = null;
            }
        }
    }
}
