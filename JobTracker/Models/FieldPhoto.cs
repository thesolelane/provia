using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class FieldPhoto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        public int? JobId { get; set; }
        public Job? Job { get; set; }

        [StringLength(100)]
        public string? Category { get; set; } // progress | issue | completion | material | safety | before | after

        [StringLength(500)]
        public string? Caption { get; set; }

        [StringLength(500)]
        public string? Tags { get; set; } // comma-separated

        // Storage: always populated with the accessible URL (local or IPFS gateway)
        [Required]
        [StringLength(1000)]
        public string Url { get; set; } = string.Empty;

        // Local storage path relative to wwwroot (e.g. /uploads/field-photos/5/abc.jpg)
        [StringLength(500)]
        public string? LocalPath { get; set; }

        // IPFS content identifier — set if pinned to IPFS via Pinata
        [StringLength(100)]
        public string? IpfsCid { get; set; }

        // Which backend was used
        [StringLength(20)]
        public string StorageBackend { get; set; } = "local"; // local | ipfs

        [StringLength(100)]
        public string? OriginalFileName { get; set; }
        public long FileSizeBytes { get; set; }

        [StringLength(50)]
        public string? ContentType { get; set; }

        // Optional GPS from mobile browser
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }

        [StringLength(100)]
        public string? UploadedBy { get; set; }

        public DateTime TakenAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class PhotoCategories
    {
        public const string Progress   = "progress";
        public const string Issue      = "issue";
        public const string Completion = "completion";
        public const string Material   = "material";
        public const string Safety     = "safety";
        public const string Before     = "before";
        public const string After      = "after";
        public const string General    = "general";

        public static readonly string[] All = { Progress, Issue, Completion, Material, Safety, Before, After, General };
    }
}
