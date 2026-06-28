using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FieldPhotosController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FieldPhotosController> _logger;

        public FieldPhotosController(
            JobTrackerContext context,
            ITenantContext tenantContext,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory,
            ILogger<FieldPhotosController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _env = env;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private string GetUser() =>
            User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        // ── Helper: load or create the company's storage config ──────────────────
        private async Task<PhotoStorageConfig> GetOrCreateConfig(int companyId)
        {
            var cfg = await _context.PhotoStorageConfigs.FirstOrDefaultAsync(c => c.CompanyId == companyId);
            if (cfg != null) return cfg;
            cfg = new PhotoStorageConfig { CompanyId = companyId };
            _context.PhotoStorageConfigs.Add(cfg);
            await _context.SaveChangesAsync();
            return cfg;
        }

        // ── Helper: resolve the physical storage directory for a company ──────────
        private string ResolveStorageDir(PhotoStorageConfig cfg, int companyId)
        {
            if (!string.IsNullOrWhiteSpace(cfg.StoragePath))
            {
                // Absolute or relative path the admin configured
                var path = cfg.StoragePath.Trim();
                return Path.IsPathRooted(path)
                    ? Path.Combine(path, companyId.ToString())
                    : Path.Combine(_env.WebRootPath, path, companyId.ToString());
            }
            // Default: wwwroot/uploads/field-photos/{companyId}
            return Path.Combine(_env.WebRootPath, "uploads", "field-photos", companyId.ToString());
        }

        // ── Helper: build the public URL for a locally stored file ───────────────
        private string BuildLocalUrl(PhotoStorageConfig cfg, int companyId, string fileName)
        {
            if (!string.IsNullOrWhiteSpace(cfg.StoragePath) && Path.IsPathRooted(cfg.StoragePath.Trim()))
                // Absolute path — serve via the /uploads/field-photos proxy endpoint
                return $"/uploads/field-photos/{companyId}/{fileName}";
            return !string.IsNullOrWhiteSpace(cfg.StoragePath)
                ? $"/{cfg.StoragePath.Trim().TrimStart('/')}/{companyId}/{fileName}"
                : $"/uploads/field-photos/{companyId}/{fileName}";
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // SETTINGS endpoints
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // GET: api/fieldphotos/settings
        [HttpGet("settings")]
        public async Task<ActionResult<object>> GetSettings()
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var cfg = await GetOrCreateConfig(companyId);
                var pinataConfigured = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PINATA_JWT"));
                return Ok(new
                {
                    cfg.StoragePath,
                    cfg.AllowedExtensions,
                    cfg.MaxFileSizeMb,
                    cfg.EnableIpfs,
                    PinataConfigured = pinataConfigured,
                    EffectiveStorageDir = ResolveStorageDir(cfg, companyId),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching photo settings");
                return StatusCode(500, new { message = "Error fetching settings" });
            }
        }

        // PUT: api/fieldphotos/settings
        [HttpPut("settings")]
        public async Task<ActionResult> UpdateSettings([FromBody] PhotoSettingsRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var cfg = await GetOrCreateConfig(companyId);

                // Validate max file size (1–200 MB)
                if (req.MaxFileSizeMb < 1 || req.MaxFileSizeMb > 200)
                    return BadRequest(new { message = "Max file size must be between 1 and 200 MB" });

                // Validate extensions — only known image types allowed
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "jpg","jpeg","png","webp","heic","heif","gif","bmp","tiff" };
                var exts = req.AllowedExtensions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(e => e.TrimStart('.').ToLower());
                var invalid = exts.Where(e => !allowed.Contains(e)).ToList();
                if (invalid.Any())
                    return BadRequest(new { message = $"Unsupported file types: {string.Join(", ", invalid)}" });

                // If a custom path is set, validate we can write to it
                if (!string.IsNullOrWhiteSpace(req.StoragePath))
                {
                    var testDir = Path.IsPathRooted(req.StoragePath.Trim())
                        ? Path.Combine(req.StoragePath.Trim(), companyId.ToString())
                        : Path.Combine(_env.WebRootPath, req.StoragePath.Trim(), companyId.ToString());
                    try { Directory.CreateDirectory(testDir); }
                    catch (Exception)
                    {
                        return BadRequest(new { message = $"Cannot create directory at path: {req.StoragePath}. Check permissions." });
                    }
                }

                cfg.StoragePath        = string.IsNullOrWhiteSpace(req.StoragePath) ? null : req.StoragePath.Trim();
                cfg.AllowedExtensions  = string.Join(",", exts.Distinct());
                cfg.MaxFileSizeMb      = req.MaxFileSizeMb;
                cfg.EnableIpfs         = req.EnableIpfs && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PINATA_JWT"));

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    cfg.StoragePath,
                    cfg.AllowedExtensions,
                    cfg.MaxFileSizeMb,
                    cfg.EnableIpfs,
                    EffectiveStorageDir = ResolveStorageDir(cfg, companyId),
                    message = "Settings saved",
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating photo settings");
                return StatusCode(500, new { message = "Error saving settings" });
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // PHOTO CRUD
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // GET: api/fieldphotos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPhotos(
            [FromQuery] int? jobId       = null,
            [FromQuery] string? category = null,
            [FromQuery] string? search   = null,
            [FromQuery] int page         = 1,
            [FromQuery] int pageSize     = 48)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var query = _context.FieldPhotos.Where(p => p.CompanyId == companyId);

                if (jobId.HasValue) query = query.Where(p => p.JobId == jobId);
                if (!string.IsNullOrWhiteSpace(category) && category != "all")
                    query = query.Where(p => p.Category == category);
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(p =>
                        (p.Caption != null && p.Caption.Contains(search)) ||
                        (p.Tags    != null && p.Tags.Contains(search)));

                var total = await query.CountAsync();
                var photos = await query
                    .OrderByDescending(p => p.TakenAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id, p.Url, p.LocalPath, p.IpfsCid, p.StorageBackend,
                        p.Category, p.Caption, p.Tags, p.OriginalFileName,
                        p.FileSizeBytes, p.Latitude, p.Longitude,
                        p.UploadedBy, p.TakenAt, p.CreatedAt,
                        p.JobId,
                        JobNumber = p.Job != null ? p.Job.JobNumber : null,
                        JobName   = p.Job != null ? p.Job.Name      : null,
                    })
                    .ToListAsync();

                return Ok(new { total, page, pageSize, photos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching field photos");
                return StatusCode(500, new { message = "Error fetching photos" });
            }
        }

        // GET: api/fieldphotos/summary
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSummary()
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var photos = await _context.FieldPhotos.Where(p => p.CompanyId == companyId).ToListAsync();
                return Ok(new
                {
                    Total          = photos.Count,
                    TotalSizeBytes = photos.Sum(p => p.FileSizeBytes),
                    IpfsCount      = photos.Count(p => p.StorageBackend == "ipfs"),
                    LocalCount     = photos.Count(p => p.StorageBackend == "local"),
                    ThisWeek       = photos.Count(p => p.TakenAt >= DateTime.UtcNow.AddDays(-7)),
                    ByCategoryJson = JsonSerializer.Serialize(
                        photos.GroupBy(p => p.Category ?? "general")
                              .ToDictionary(g => g.Key, g => g.Count())),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching photo summary");
                return StatusCode(500, new { message = "Error fetching summary" });
            }
        }

        // POST: api/fieldphotos/upload
        [HttpPost("upload")]
        [RequestSizeLimit(210 * 1024 * 1024)] // ceiling at 210 MB to allow up to 200 MB setting
        public async Task<ActionResult<object>> Upload(
            [FromForm] IFormFile file,
            [FromForm] int? jobId         = null,
            [FromForm] string? category   = null,
            [FromForm] string? caption    = null,
            [FromForm] string? tags       = null,
            [FromForm] decimal? latitude  = null,
            [FromForm] decimal? longitude = null,
            [FromForm] string? takenAt    = null)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var cfg = await GetOrCreateConfig(companyId);
                var allowedMimes = cfg.GetAllowedMimeTypes();

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file provided" });
                if (file.Length > cfg.GetMaxBytes())
                    return BadRequest(new { message = $"File too large — your limit is {cfg.MaxFileSizeMb} MB" });
                if (!allowedMimes.Contains(file.ContentType.ToLower()))
                {
                    var extList = cfg.AllowedExtensions.ToUpper();
                    return BadRequest(new { message = $"File type not allowed. Your allowed types: {extList}" });
                }

                // ── Determine storage directory ──────────────────────────────────
                var storageDir = ResolveStorageDir(cfg, companyId);
                Directory.CreateDirectory(storageDir);

                var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
                var safeName = $"{Guid.NewGuid():N}{ext}";
                var diskPath = Path.Combine(storageDir, safeName);

                await using (var fs = System.IO.File.Create(diskPath))
                    await file.CopyToAsync(fs);

                var localPath = BuildLocalUrl(cfg, companyId, safeName);
                var url       = localPath;
                var backend   = "local";
                string? ipfsCid = null;

                // ── Optional IPFS via Pinata ─────────────────────────────────────
                var pinataJwt = Environment.GetEnvironmentVariable("PINATA_JWT");
                if (cfg.EnableIpfs && !string.IsNullOrWhiteSpace(pinataJwt))
                {
                    try
                    {
                        var cid = await PinToPinataAsync(diskPath, safeName, file.ContentType, pinataJwt);
                        if (cid != null)
                        {
                            ipfsCid = cid;
                            url     = $"https://gateway.pinata.cloud/ipfs/{cid}";
                            backend = "ipfs";
                        }
                    }
                    catch (Exception pinEx)
                    {
                        _logger.LogWarning(pinEx, "Pinata upload failed, keeping local copy");
                    }
                }

                var takenParsed = DateTime.TryParse(takenAt, out var takenDt)
                    ? takenDt.ToUniversalTime()
                    : DateTime.UtcNow;

                var photo = new FieldPhoto
                {
                    CompanyId        = companyId,
                    JobId            = jobId,
                    Category         = category?.Trim() ?? "general",
                    Caption          = caption?.Trim(),
                    Tags             = tags?.Trim(),
                    Url              = url,
                    LocalPath        = localPath,
                    IpfsCid          = ipfsCid,
                    StorageBackend   = backend,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    FileSizeBytes    = file.Length,
                    ContentType      = file.ContentType,
                    Latitude         = latitude,
                    Longitude        = longitude,
                    UploadedBy       = GetUser(),
                    TakenAt          = takenParsed,
                };

                _context.FieldPhotos.Add(photo);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    photo.Id, photo.Url, photo.LocalPath, photo.IpfsCid,
                    photo.StorageBackend, photo.Category, photo.Caption,
                    photo.TakenAt, photo.FileSizeBytes,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading field photo");
                return StatusCode(500, new { message = "Error uploading photo" });
            }
        }

        // PUT: api/fieldphotos/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePhoto(int id, [FromBody] UpdatePhotoRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var photo = await _context.FieldPhotos.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
                if (photo == null) return NotFound();

                photo.Category = req.Category?.Trim() ?? photo.Category;
                photo.Caption  = req.Caption?.Trim();
                photo.Tags     = req.Tags?.Trim();
                if (req.JobId.HasValue) photo.JobId = req.JobId;

                await _context.SaveChangesAsync();
                return Ok(new { photo.Id, photo.Category, photo.Caption, photo.Tags });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating photo {Id}", id);
                return StatusCode(500, new { message = "Error updating photo" });
            }
        }

        // DELETE: api/fieldphotos/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePhoto(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var photo = await _context.FieldPhotos.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
                if (photo == null) return NotFound();

                // Delete the file from disk if it exists
                if (!string.IsNullOrEmpty(photo.LocalPath))
                {
                    // Attempt to resolve the physical file
                    string diskPath;
                    var cfg = await _context.PhotoStorageConfigs.FirstOrDefaultAsync(c => c.CompanyId == companyId);
                    if (cfg != null && !string.IsNullOrWhiteSpace(cfg.StoragePath) && Path.IsPathRooted(cfg.StoragePath))
                    {
                        // Absolute path — filename is last segment of LocalPath
                        diskPath = Path.Combine(cfg.StoragePath.Trim(), companyId.ToString(), Path.GetFileName(photo.LocalPath));
                    }
                    else
                    {
                        diskPath = Path.Combine(_env.WebRootPath, photo.LocalPath.TrimStart('/'));
                    }
                    if (System.IO.File.Exists(diskPath))
                        System.IO.File.Delete(diskPath);
                }

                _context.FieldPhotos.Remove(photo);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Photo deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo {Id}", id);
                return StatusCode(500, new { message = "Error deleting photo" });
            }
        }

        // ── Pinata IPFS helper ────────────────────────────────────────────────────
        private async Task<string?> PinToPinataAsync(string filePath, string fileName, string contentType, string jwt)
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            await using var fs = System.IO.File.OpenRead(filePath);
            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent("{\"name\":\"" + fileName + "\"}"), "pinataMetadata");

            var resp = await client.PostAsync("https://api.pinata.cloud/pinning/pinFileToIPFS", form);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("IpfsHash", out var hash) ? hash.GetString() : null;
        }
    }

    // ─── Request DTOs ────────────────────────────────────────────────────────────
    public class UpdatePhotoRequest
    {
        public string? Category { get; set; }
        public string? Caption  { get; set; }
        public string? Tags     { get; set; }
        public int?    JobId    { get; set; }
    }

    public class PhotoSettingsRequest
    {
        public string  StoragePath       { get; set; } = "";
        public string  AllowedExtensions { get; set; } = "jpg,jpeg,png,webp,heic,heif";
        public int     MaxFileSizeMb     { get; set; } = 20;
        public bool    EnableIpfs        { get; set; } = false;
    }
}
