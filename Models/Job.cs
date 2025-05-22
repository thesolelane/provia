using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class Job
    {
        [Key]
        public int JobId { get; set; }

        [Required]
        [MaxLength(100)]
        public string JobName { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; }

        [MaxLength(50)]
        public string ClientName { get; set; }

        [MaxLength(50)]
        public string ClientPhone { get; set; }

        [MaxLength(100)]
        public string ClientEmail { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? ExpectedCompletionDate { get; set; }

        public DateTime? ActualCompletionDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }

        [Required]
        public JobStatus Status { get; set; }

        [MaxLength(100)]
        public string ProjectManager { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual ICollection<JobSection> Sections { get; set; } = new List<JobSection>();
        public virtual ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

        // Auditing
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    public enum JobStatus
    {
        Planned,
        InProgress,
        OnHold,
        Completed,
        Cancelled
    }
}
