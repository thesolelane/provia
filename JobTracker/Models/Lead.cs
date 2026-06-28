using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class Lead
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        [Required]
        [StringLength(50)]
        public string LeadNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string CallerName { get; set; } = string.Empty;

        [StringLength(30)]
        public string? CallerPhone { get; set; }

        [StringLength(200)]
        public string? CallerEmail { get; set; }

        [StringLength(100)]
        public string? Source { get; set; }

        [Required]
        [StringLength(50)]
        public string Stage { get; set; } = "incoming";

        public DateTime StageEnteredAt { get; set; } = DateTime.UtcNow;

        [StringLength(300)]
        public string? JobAddress { get; set; }

        [StringLength(100)]
        public string? JobCity { get; set; }

        [StringLength(50)]
        public string? JobType { get; set; }

        [StringLength(2000)]
        public string? JobScope { get; set; }

        public DateTime? AppointmentAt { get; set; }

        public bool IsArchived { get; set; } = false;

        [StringLength(100)]
        public string? ArchiveReason { get; set; }

        // Graduation links
        public int? ContactId { get; set; }
        public Contact? Contact { get; set; }

        public int? JobId { get; set; }
        public Job? Job { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<LeadNote> Notes { get; set; } = new List<LeadNote>();
    }

    public static class LeadStages
    {
        public static readonly string[] All = {
            "incoming", "callback_done", "appointment_booked",
            "site_visit_done", "quote_sent", "follow_up", "signed"
        };

        public static readonly Dictionary<string, string> Labels = new()
        {
            ["incoming"] = "New Lead",
            ["callback_done"] = "Callback Done",
            ["appointment_booked"] = "Appt. Booked",
            ["site_visit_done"] = "Site Visited",
            ["quote_sent"] = "Quote Sent",
            ["follow_up"] = "Following Up",
            ["signed"] = "Signed"
        };

        // Days before a lead is considered stale at this stage
        public static readonly Dictionary<string, int> StaleDays = new()
        {
            ["incoming"] = 1,
            ["callback_done"] = 2,
            ["appointment_booked"] = 2,
            ["site_visit_done"] = 3,
            ["quote_sent"] = 7,
            ["follow_up"] = 7,
            ["signed"] = 30
        };
    }
}
