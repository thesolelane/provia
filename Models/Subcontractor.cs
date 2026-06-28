using System.ComponentModel.DataAnnotations;

namespace JobTrackerApp.Models
{
    public class Subcontractor
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(100)]
        public string ContactName { get; set; } = string.Empty;

        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string LicenseNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string InsuranceInfo { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<JobSection> Sections { get; set; } = new List<JobSection>();
        public ICollection<SectionSubcontractor> SectionSubcontractors { get; set; } = new List<SectionSubcontractor>();
    }
}
