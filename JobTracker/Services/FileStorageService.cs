using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace JobTracker.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string directory);
        Task<string?> CreateThumbnailAsync(IFormFile file, string originalFilePath, string directory);
        Task DeleteFileAsync(string filePath);
        string GetFileUrl(string filePath);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _rootPath;
        private readonly string _baseUrl;

        public FileStorageService(IWebHostEnvironment environment, IConfiguration configuration, ILogger<FileStorageService> logger)
        {
            _logger = logger;
            _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");
            _baseUrl = configuration["FileStorage:BaseUrl"] ?? "/uploads";
            
            // Ensure upload directory exists
            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, string directory)
        {
            try
            {
                // Create full directory path if it doesn't exist
                string fullDirectory = Path.Combine(_rootPath, directory);
                if (!Directory.Exists(fullDirectory))
                {
                    Directory.CreateDirectory(fullDirectory);
                }

                // Generate a unique filename
                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName.Replace(" ", "_"))}";
                string filePath = Path.Combine(directory, fileName);
                string fullPath = Path.Combine(_rootPath, filePath);

                // Save the file
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file");
                throw;
            }
        }

        public async Task<string?> CreateThumbnailAsync(IFormFile file, string originalFilePath, string directory)
        {
            try
            {
                // Check if file is an image
                if (!IsImage(file.ContentType))
                {
                    return null;
                }

                // Create thumbnail directory if it doesn't exist
                string thumbDirectory = Path.Combine(_rootPath, directory, "thumbnails");
                if (!Directory.Exists(thumbDirectory))
                {
                    Directory.CreateDirectory(thumbDirectory);
                }

                // Generate thumbnail filename
                string fileName = Path.GetFileName(originalFilePath);
                string thumbFileName = $"thumb_{fileName}";
                string thumbPath = Path.Combine(directory, "thumbnails", thumbFileName);
                string fullThumbPath = Path.Combine(_rootPath, directory, "thumbnails", thumbFileName);

                // For this simplified version, we'll just copy the file as a thumbnail
                // In a production environment, you would use a proper image processing library
                using (var sourceStream = file.OpenReadStream())
                using (var destinationStream = new FileStream(fullThumbPath, FileMode.Create))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

                _logger.LogInformation($"Created thumbnail at {thumbPath}");
                return thumbPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating thumbnail");
                return null;
            }
        }

        public async Task DeleteFileAsync(string filePath)
        {
            string fullPath = Path.Combine(_rootPath, filePath);
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
            }

            // Also try to delete thumbnail if it exists
            string directory = Path.GetDirectoryName(filePath) ?? "";
            string fileName = Path.GetFileName(filePath);
            string thumbPath = Path.Combine(_rootPath, directory, "thumbnails", $"thumb_{fileName}");
            
            if (File.Exists(thumbPath))
            {
                await Task.Run(() => File.Delete(thumbPath));
            }
        }

        public string GetFileUrl(string filePath)
        {
            return $"{_baseUrl}/{filePath.Replace("\\", "/")}";
        }

        private bool IsImage(string contentType)
        {
            return contentType.StartsWith("image/");
        }
    }
}