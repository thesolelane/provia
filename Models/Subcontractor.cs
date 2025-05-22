using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class Subcontractor
    {
        [Key]
        public int SubcontractorId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CompanyName { get; set; }

        [MaxLength(100)]
        public string ContactName { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; }

        [MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(200)]
        public string Address { get; set; }

        [MaxLength(50)]
        public string LicenseNumber { get; set; }

        [MaxLength(200)]
        public string InsuranceInfo { get; set; }

        [MaxLength(100)]
        public string Specialty { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual ICollection<JobSection> JobSections { get; set; } = new List<JobSection>();

        // Auditing
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
