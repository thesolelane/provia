using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class MaterialRun
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int JobId { get; set; }
        public Job Job { get; set; } = null!;

        [Required]
        public int MaterialStoreId { get; set; }
        public MaterialStore MaterialStore { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Purpose { get; set; } = string.Empty; // What materials were being picked up

        // Travel tracking
        public DateTime? DepartureTime { get; set; }
        public DateTime? ArrivalAtStoreTime { get; set; }
        public DateTime? DepartureFromStoreTime { get; set; }
        public DateTime? ReturnTime { get; set; }

        // GPS coordinates for departure (job site)
        public double? DepartureLatitude { get; set; }
        public double? DepartureLongitude { get; set; }

        // GPS coordinates for return (job site)
        public double? ReturnLatitude { get; set; }
        public double? ReturnLongitude { get; set; }

        // Calculated times
        [Column(TypeName = "decimal(5,2)")]
        public decimal? TravelTimeHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? StoreTimeHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? TotalTimeHours { get; set; }

        // Verification flags
        public bool LocationVerified { get; set; } = false;
        public bool IsCompleted { get; set; } = false;

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}