using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class LeadNote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LeadId { get; set; }
        public Lead? Lead { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(4000)]
        public string Body { get; set; } = string.Empty;

        [StringLength(200)]
        public string? UserName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
