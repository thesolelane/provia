using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class TimeEntry
    {
        [Key]
        public int TimeEntryId { get; set; }

        [Required]
        public string UserId { get; set; }

        public int? JobId { get; set; }

        public int? JobSectionId { get; set; }

        [Required]
        public DateTime ClockInTime { get; set; }

        public DateTime? ClockOutTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalHours { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("JobId")]
        public virtual Job Job { get; set; }

        [ForeignKey("JobSectionId")]
        public virtual JobSection JobSection { get; set; }

        // Auditing
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
