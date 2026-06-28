using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class ContactActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ContactId { get; set; }
        public Contact? Contact { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Detail { get; set; }

        [StringLength(200)]
        public string? UserName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
