using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using System.Net;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobImagesController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<JobImagesController> _logger;

        public JobImagesController(
            JobTrackerContext context, 
            IFileStorageService fileStorage,
            ILogger<JobImagesController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // GET: api/JobImages/Job/5
        [HttpGet("Job/{jobId}")]
        public async Task<ActionResult<IEnumerable<JobImage>>> GetJobImages(int jobId)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(jobId);
                if (job == null)
                {
                    return NotFound($"Job with ID {jobId} not found");
                }

                var images = await _context.JobImages
                    .Where(i => i.JobId == jobId && i.JobSectionId == null)
                    .OrderBy(i => i.DisplayOrder)
                    .ToListAsync();

                return images;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting images for job ID {jobId}");
                return StatusCode(500, "An error occurred while retrieving job images");
            }
        }

        // GET: api/JobImages/Section/5
        [HttpGet("Section/{sectionId}")]
        public async Task<ActionResult<IEnumerable<JobImage>>> GetSectionImages(int sectionId)
        {
            try
            {
                var section = await _context.JobSections.FindAsync(sectionId);
                if (section == null)
                {
                    return NotFound($"Job section with ID {sectionId} not found");
                }

                var images = await _context.JobImages
                    .Where(i => i.JobSectionId == sectionId)
                    .OrderBy(i => i.DisplayOrder)
                    .ToListAsync();

                return images;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting images for section ID {sectionId}");
                return StatusCode(500, "An error occurred while retrieving section images");
            }
        }

        // GET: api/JobImages/MainImage/5
        [HttpGet("MainImage/{jobId}")]
        public async Task<ActionResult<JobImage>> GetMainJobImage(int jobId)
        {
            try
            {
                var mainImage = await _context.JobImages
                    .Where(i => i.JobId == jobId && i.IsMainImage)
                    .FirstOrDefaultAsync();

                if (mainImage == null)
                {
                    return NotFound($"No main image found for job ID {jobId}");
                }

                return mainImage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting main image for job ID {jobId}");
                return StatusCode(500, "An error occurred while retrieving the main job image");
            }
        }

        // POST: api/JobImages/UploadJobImage/5
        [HttpPost("UploadJobImage/{jobId}")]
        public async Task<ActionResult<JobImage>> UploadJobImage(int jobId, [FromForm] IFormFile file, [FromForm] string? description = null, [FromForm] bool isMainImage = false)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(jobId);
                if (job == null)
                {
                    return NotFound($"Job with ID {jobId} not found");
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                // Get highest display order for the job
                int displayOrder = await _context.JobImages
                    .Where(i => i.JobId == jobId && i.JobSectionId == null)
                    .Select(i => i.DisplayOrder)
                    .DefaultIfEmpty()
                    .MaxAsync() + 1;

                // Save file
                string directory = $"jobs/{jobId}";
                string filePath = await _fileStorage.SaveFileAsync(file, directory);
                
                // Create thumbnail (optional)
                string? thumbnailPath = await _fileStorage.CreateThumbnailAsync(file, filePath, directory);

                // Create image record
                var jobImage = new JobImage
                {
                    JobId = jobId,
                    FileName = Path.GetFileName(file.FileName),
                    StoragePath = filePath,
                    ContentType = file.ContentType,
                    Description = description,
                    IsMainImage = isMainImage,
                    DisplayOrder = displayOrder,
                    FileSize = file.Length,
                    ThumbnailPath = thumbnailPath
                };

                // If this is set as main image, clear other main images
                if (isMainImage)
                {
                    var currentMainImages = await _context.JobImages
                        .Where(i => i.JobId == jobId && i.IsMainImage)
                        .ToListAsync();
                    
                    foreach (var image in currentMainImages)
                    {
                        image.IsMainImage = false;
                    }
                }

                _context.JobImages.Add(jobImage);
                await _context.SaveChangesAsync();

                // Return file URL and created image
                jobImage.StoragePath = _fileStorage.GetFileUrl(filePath);
                if (!string.IsNullOrEmpty(thumbnailPath))
                {
                    jobImage.ThumbnailPath = _fileStorage.GetFileUrl(thumbnailPath);
                }

                return CreatedAtAction(nameof(GetJobImages), new { jobId }, jobImage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading image for job ID {jobId}");
                return StatusCode(500, "An error occurred while uploading the image");
            }
        }

        // POST: api/JobImages/UploadSectionImage/5
        [HttpPost("UploadSectionImage/{sectionId}")]
        public async Task<ActionResult<JobImage>> UploadSectionImage(int sectionId, [FromForm] IFormFile file, [FromForm] string? description = null)
        {
            try
            {
                var section = await _context.JobSections
                    .Include(s => s.Job)
                    .FirstOrDefaultAsync(s => s.Id == sectionId);
                
                if (section == null)
                {
                    return NotFound($"Job section with ID {sectionId} not found");
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                // Get highest display order for the section
                int displayOrder = await _context.JobImages
                    .Where(i => i.JobSectionId == sectionId)
                    .Select(i => i.DisplayOrder)
                    .DefaultIfEmpty()
                    .MaxAsync() + 1;

                // Check if we already have max images (5)
                int currentImageCount = await _context.JobImages
                    .CountAsync(i => i.JobSectionId == sectionId);
                
                if (currentImageCount >= 5)
                {
                    return BadRequest("Maximum number of images (5) reached for this section");
                }

                // Save file
                string directory = $"jobs/{section.JobId}/sections/{sectionId}";
                string filePath = await _fileStorage.SaveFileAsync(file, directory);
                
                // Create thumbnail (optional)
                string? thumbnailPath = await _fileStorage.CreateThumbnailAsync(file, filePath, directory);

                // Is this a video?
                bool isVideo = file.ContentType.StartsWith("video/");

                // Create image record
                var jobImage = new JobImage
                {
                    JobId = section.JobId,
                    JobSectionId = sectionId,
                    FileName = Path.GetFileName(file.FileName),
                    StoragePath = filePath,
                    ContentType = file.ContentType,
                    Description = description,
                    IsMainImage = false,
                    IsVideo = isVideo,
                    DisplayOrder = displayOrder,
                    FileSize = file.Length,
                    ThumbnailPath = thumbnailPath
                };

                _context.JobImages.Add(jobImage);
                await _context.SaveChangesAsync();

                // Return file URL and created image
                jobImage.StoragePath = _fileStorage.GetFileUrl(filePath);
                if (!string.IsNullOrEmpty(thumbnailPath))
                {
                    jobImage.ThumbnailPath = _fileStorage.GetFileUrl(thumbnailPath);
                }

                return CreatedAtAction(nameof(GetSectionImages), new { sectionId }, jobImage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading image for section ID {sectionId}");
                return StatusCode(500, "An error occurred while uploading the image");
            }
        }

        // DELETE: api/JobImages/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJobImage(int id)
        {
            try
            {
                var jobImage = await _context.JobImages.FindAsync(id);
                if (jobImage == null)
                {
                    return NotFound($"Image with ID {id} not found");
                }

                // Delete physical file
                await _fileStorage.DeleteFileAsync(jobImage.StoragePath);

                // Delete database record
                _context.JobImages.Remove(jobImage);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image with ID {id}");
                return StatusCode(500, "An error occurred while deleting the image");
            }
        }

        // PUT: api/JobImages/SetMainImage/5
        [HttpPut("SetMainImage/{id}")]
        public async Task<IActionResult> SetMainImage(int id)
        {
            try
            {
                var jobImage = await _context.JobImages.FindAsync(id);
                if (jobImage == null)
                {
                    return NotFound($"Image with ID {id} not found");
                }

                // Clear other main images for this job
                var currentMainImages = await _context.JobImages
                    .Where(i => i.JobId == jobImage.JobId && i.IsMainImage && i.Id != id)
                    .ToListAsync();
                
                foreach (var image in currentMainImages)
                {
                    image.IsMainImage = false;
                }

                // Set this as main image
                jobImage.IsMainImage = true;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting main image with ID {id}");
                return StatusCode(500, "An error occurred while setting the main image");
            }
        }

        // PUT: api/JobImages/UpdateOrder
        [HttpPut("UpdateOrder")]
        public async Task<IActionResult> UpdateDisplayOrder([FromBody] List<ImageOrderUpdate> updates)
        {
            try
            {
                foreach (var update in updates)
                {
                    var image = await _context.JobImages.FindAsync(update.ImageId);
                    if (image != null)
                    {
                        image.DisplayOrder = update.NewOrder;
                    }
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating image display order");
                return StatusCode(500, "An error occurred while updating image display order");
            }
        }
    }

    public class ImageOrderUpdate
    {
        public int ImageId { get; set; }
        public int NewOrder { get; set; }
    }
}