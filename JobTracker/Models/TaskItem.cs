using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class TaskItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        // Assignment
        public int? AssignedToUserId { get; set; }

        [StringLength(200)]
        public string? AssignedToName { get; set; }

        // Optional links
        public int? JobId { get; set; }
        public Job? Job { get; set; }

        public int? ContactId { get; set; }
        public Contact? Contact { get; set; }

        public int? LeadId { get; set; }
        public Lead? Lead { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "pending"; // pending/in_progress/complete/cancelled

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "medium"; // low/medium/high/urgent

        // Recurrence
        public bool IsRecurring { get; set; } = false;

        [StringLength(30)]
        public string? RecurrenceRule { get; set; } // daily/weekly/biweekly/monthly/yearly

        // Chain: each completed recurring task spawns a child
        public int? ParentTaskId { get; set; }
        public TaskItem? ParentTask { get; set; }

        // Completion
        public DateTime? CompletedAt { get; set; }

        [StringLength(200)]
        public string? CompletedBy { get; set; }

        [StringLength(1000)]
        public string? CompletionNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class RecurrenceRules
    {
        public static DateTime NextDue(DateTime currentDue, string rule) => rule switch
        {
            "daily"     => currentDue.AddDays(1),
            "weekly"    => currentDue.AddDays(7),
            "biweekly"  => currentDue.AddDays(14),
            "monthly"   => currentDue.AddMonths(1),
            "yearly"    => currentDue.AddYears(1),
            _           => currentDue.AddDays(7)
        };
    }
}
