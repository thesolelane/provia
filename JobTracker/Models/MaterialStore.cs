using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class MaterialStore
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string StoreName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string StoreType { get; set; } = string.Empty; // "Hardware", "Lumber", "Electrical", "Plumbing", etc.

        [Required]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [StringLength(20)]
        public string ZipCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Distance from main office (37 Duckmill Rd Fitchburg MA 01420)
        public double DistanceFromOffice { get; set; }

        public bool IsActive { get; set; } = true;

        // Store hours and additional info
        [StringLength(500)]
        public string Hours { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}