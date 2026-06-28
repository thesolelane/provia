using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class Contact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        [Required]
        [StringLength(200)]
        public string CustomerNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? CompanyName { get; set; }

        [StringLength(20)]
        public string ContactType { get; set; } = "Residential";

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? ZipCode { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Job> Jobs { get; set; } = new List<Job>();
        public ICollection<ContactDocument> Documents { get; set; } = new List<ContactDocument>();
        public ICollection<ContactActivityLog> ActivityLogs { get; set; } = new List<ContactActivityLog>();
    }
}
