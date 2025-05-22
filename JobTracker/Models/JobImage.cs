using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JobTracker.Models
{
    public class JobImage
    {
        public int Id { get; set; }
        
        [Required]
        public int JobId { get; set; }
        
        [JsonIgnore]
        public Job Job { get; set; } = null!;
        
        // Optional SectionId if the image belongs to a specific section
        public int? JobSectionId { get; set; }
        
        [JsonIgnore]
        public JobSection? JobSection { get; set; }
        
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string StoragePath { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string ContentType { get; set; } = "image/jpeg";
        
        [StringLength(255)]
        public string? Description { get; set; }
        
        // Indicates if this is the main display image for the job
        public bool IsMainImage { get; set; }
        
        // Indicates if this is a video instead of an image
        public bool IsVideo { get; set; }
        
        // For sorting/ordering images
        public int DisplayOrder { get; set; } = 0;
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        // Size in bytes
        public long FileSize { get; set; }
        
        // Optional thumbnail path for faster loading
        [StringLength(255)]
        public string? ThumbnailPath { get; set; }
        
        // Image dimensions if applicable
        public int? Width { get; set; }
        public int? Height { get; set; }
        
        // Image number in sequence (for each job or section)
        public int ImageNumber { get; set; }
        
        // Flag to show timestamp overlay on image
        public bool ShowTimestamp { get; set; } = true;
    }
}