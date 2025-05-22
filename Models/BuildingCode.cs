using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class BuildingCode
    {
        [Key]
        public int CodeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CodeNumber { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public SectionType RelatedSection { get; set; }

        [MaxLength(100)]
        public string Category { get; set; }

        [MaxLength(100)]
        public string Subcategory { get; set; }

        public string FullText { get; set; }

        [MaxLength(200)]
        public string Source { get; set; }

        [MaxLength(50)]
        public string Version { get; set; }

        [MaxLength(100)]
        public string Jurisdiction { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string Notes { get; set; }

        // Auditing
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
