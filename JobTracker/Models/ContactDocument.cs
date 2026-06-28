using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class ContactDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ContactId { get; set; }
        public Contact? Contact { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(500)]
        public string FileName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? OriginalName { get; set; }

        [StringLength(100)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        [StringLength(200)]
        public string? UploadedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
