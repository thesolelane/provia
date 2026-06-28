using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    /// <summary>
    /// Per-company photo storage preferences.
    /// Stored in DB — admins edit via the Photos > Settings panel.
    /// </summary>
    public class PhotoStorageConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        /// <summary>
        /// Absolute path on the host machine for local storage.
        /// Examples: /mnt/nas/provia-photos  /data/photos  /var/provia/uploads
        /// Leave blank to use the default wwwroot/uploads/field-photos/{companyId} folder.
        /// </summary>
        [StringLength(500)]
        public string? StoragePath { get; set; }

        /// <summary>
        /// Comma-separated list of allowed extensions WITHOUT dots.
        /// e.g. "jpg,jpeg,png,webp,heic"
        /// </summary>
        [StringLength(200)]
        public string AllowedExtensions { get; set; } = "jpg,jpeg,png,webp,heic,heif";

        /// <summary>Maximum file size in megabytes (1–100).</summary>
        public int MaxFileSizeMb { get; set; } = 20;

        /// <summary>When true, photos are also pinned to IPFS via Pinata (requires PINATA_JWT env var).</summary>
        public bool EnableIpfs { get; set; } = false;

        /// <summary>Allowed MIME types derived from AllowedExtensions.</summary>
        public static readonly Dictionary<string, string> ExtToMime = new(StringComparer.OrdinalIgnoreCase)
        {
            ["jpg"]  = "image/jpeg",
            ["jpeg"] = "image/jpeg",
            ["png"]  = "image/png",
            ["webp"] = "image/webp",
            ["heic"] = "image/heic",
            ["heif"] = "image/heif",
            ["gif"]  = "image/gif",
            ["bmp"]  = "image/bmp",
            ["tiff"] = "image/tiff",
        };

        public string[] GetAllowedMimeTypes()
        {
            return AllowedExtensions
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => ExtToMime.TryGetValue(e, out var m) ? m : null)
                .Where(m => m != null)
                .Select(m => m!)
                .Distinct()
                .ToArray();
        }

        public long GetMaxBytes() => (long)MaxFileSizeMb * 1024 * 1024;
    }
}
