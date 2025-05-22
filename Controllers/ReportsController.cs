using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JobTrackerApp.Data;
using JobTrackerApp.Models;
using JobTrackerApp.Services.Microsoft;
using JobTrackerApp.Services.Google;

namespace JobTrackerApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsController> _logger;
        private readonly OfficeIntegrationService _officeService;
        private readonly GoogleIntegrationService _googleService;

        public ReportsController(
            ApplicationDbContext context,
            ILogger<ReportsController> logger,
            OfficeIntegrationService officeService,
            GoogleIntegrationService googleService)
        {
            _context = context;
            _logger = logger;
            _officeService = officeService;
            _googleService = googleService;
        }

        // GET: api/Reports/job-status
        [HttpGet("job-status")]
        public async Task<ActionResult<IEnumerable<JobStatusReport>>> GetJobStatusReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                IQueryable<Job> query = _context.Jobs
                    .Include(j => j.Sections);

                // Apply date filters if provided
                if (startDate.HasValue)
                {
                    DateTime start = startDate.Value.Date;
                    query = query.Where(j => j.StartDate >= start);
                }

                if (endDate.HasValue)
                {
                    DateTime end = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                    query = query.Where(j => j.StartDate <= end);
                }

                var jobs = await query.ToListAsync();

                // Process jobs to create status report
                var statusReport = jobs.Select(j => new JobStatusReport
                {
                    JobId = j.Id,
                    JobNumber = j.JobNumber,
                    JobName = j.Name,
                    Status = j.Status,
                    StartDate = j.StartDate,
                    TargetCompletionDate = j.TargetCompletionDate,
                    ActualCompletionDate = j.ActualCompletionDate,
                    TotalSections = j.Sections.Count,
                    CompletedSections = j.Sections.Count(s => s.Status == SectionStatus.Completed || s.Status == SectionStatus.PassedInspection),
                    InProgressSections = j.Sections.Count(s => s.Status == SectionStatus.InProgress),
                    NotStartedSections = j.Sections.Count(s => s.Status == SectionStatus.NotStarted),
                    ProblemSections = j.Sections.Count(s => s.Status == SectionStatus.Denied || s.Status == SectionStatus.FailedInspection || s.Status == SectionStatus.OnHold),
                    CompletionPercentage = j.Sections.Count > 0 
                        ? (double)(j.Sections.Count(s => s.Status == SectionStatus.Completed || s.Status == SectionStatus.PassedInspection)) / j.Sections.Count * 100 
                        : 0,
                    DaysRunning = (DateTime.UtcNow - j.StartDate).Days,
                    IsOverdue = j.TargetCompletionDate.HasValue && DateTime.UtcNow > j.TargetCompletionDate.Value && j.ActualCompletionDate == null
                }).ToList();

                return Ok(statusReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating job status report");
                return StatusCode(500, "An error occurred while generating the job status report");
            }
        }

        // GET: api/Reports/time-tracking
        [HttpGet("time-tracking")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<IEnumerable<TimeTrackingReport>>> GetTimeTrackingReport(
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? jobId = null,
            [FromQuery] int? employeeId = null)
        {
            try
            {
                IQueryable<TimeEntry> query = _context.TimeEntries
                    .Include(t => t.Employee)
                    .Include(t => t.Job)
                    .Where(t => t.ClockOutTime != null); // Only completed entries

                // Apply filters
                if (startDate.HasValue)
                {
                    DateTime start = startDate.Value.Date;
                    query = query.Where(t => t.ClockInTime >= start);
                }

                if (endDate.HasValue)
                {
                    DateTime end = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                    query = query.Where(t => t.ClockInTime <= end);
                }

                if (jobId.HasValue)
                {
                    query = query.Where(t => t.JobId == jobId.Value);
                }

                if (employeeId.HasValue)
                {
                    query = query.Where(t => t.EmployeeId == employeeId.Value);
                }

                var entries = await query.ToListAsync();

                // Group by date and employee
                var report = entries
                    .GroupBy(t => new 
                    { 
                        Date = t.ClockInTime.Date,
                        EmployeeId = t.EmployeeId,
                        EmployeeName = t.Employee?.FirstName + " " + t.Employee?.LastName ?? "Unknown",
                        JobId = t.JobId,
                        JobName = t.Job?.Name ?? "No Job"
                    })
                    .Select(g => new TimeTrackingReport
                    {
                        Date = g.Key.Date,
                        EmployeeId = g.Key.EmployeeId,
                        EmployeeName = g.Key.EmployeeName,
                        JobId = g.Key.JobId,
                        JobName = g.Key.JobName,
                        TotalHours = g.Sum(t => t.TotalHours ?? 0),
                        EntryCount = g.Count(),
                        FirstClockIn = g.Min(t => t.ClockInTime),
                        LastClockOut = g.Max(t => t.ClockOutTime)
                    })
                    .OrderByDescending(r => r.Date)
                    .ThenBy(r => r.EmployeeName)
                    .ToList();

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating time tracking report");
                return StatusCode(500, "An error occurred while generating the time tracking report");
            }
        }

        // GET: api/Reports/subcontractor
        [HttpGet("subcontractor")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<IEnumerable<SubcontractorReport>>> GetSubcontractorReport(
            [FromQuery] int? subcontractorId = null,
            [FromQuery] int? jobId = null)
        {
            try
            {
                IQueryable<SectionSubcontractor> query = _context.SectionSubcontractors
                    .Include(ss => ss.Subcontractor)
                    .Include(ss => ss.Section)
                        .ThenInclude(s => s.Job);

                // Apply filters
                if (subcontractorId.HasValue)
                {
                    query = query.Where(ss => ss.SubcontractorId == subcontractorId.Value);
                }

                if (jobId.HasValue)
                {
                    query = query.Where(ss => ss.Section.JobId == jobId.Value);
                }

                var assignments = await query.ToListAsync();

                // Create report
                var report = assignments
                    .Select(ss => new SubcontractorReport
                    {
                        SubcontractorId = ss.SubcontractorId,
                        SubcontractorName = ss.Subcontractor?.CompanyName ?? "Unknown",
                        JobId = ss.Section.JobId,
                        JobName = ss.Section.Job?.Name ?? "Unknown",
                        SectionId = ss.SectionId,
                        SectionName = ss.Section.SectionType.GetDisplayName(),
                        Status = ss.Section.Status.GetStatusName(),
                        StartDate = ss.StartDate,
                        ExpectedCompletionDate = ss.ExpectedCompletionDate,
                        ActualCompletionDate = ss.ActualCompletionDate,
                        ContractAmount = ss.ContractAmount,
                        PaidAmount = ss.PaidAmount,
                        RemainingAmount = ss.ContractAmount - ss.PaidAmount,
                        InspectionDate = ss.Section.InspectionDate,
                        InspectionResult = ss.Section.InspectionResult,
                        IsCompleted = ss.ActualCompletionDate.HasValue,
                        DaysRunning = (DateTime.UtcNow - ss.StartDate).Days,
                        IsOverdue = ss.ExpectedCompletionDate.HasValue && DateTime.UtcNow > ss.ExpectedCompletionDate.Value && !ss.ActualCompletionDate.HasValue
                    })
                    .OrderBy(r => r.SubcontractorName)
                    .ThenBy(r => r.JobName)
                    .ToList();

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating subcontractor report");
                return StatusCode(500, "An error occurred while generating the subcontractor report");
            }
        }

        // GET: api/Reports/section-status
        [HttpGet("section-status")]
        public async Task<ActionResult<IEnumerable<SectionStatusReport>>> GetSectionStatusReport([FromQuery] int? jobId = null)
        {
            try
            {
                IQueryable<JobSection> query = _context.JobSections
                    .Include(s => s.Job)
                    .Include(s => s.ResponsibleEmployee)
                    .Include(s => s.Subcontractor);

                if (jobId.HasValue)
                {
                    query = query.Where(s => s.JobId == jobId.Value);
                }

                var sections = await query.ToListAsync();

                // Create report
                var report = sections
                    .Select(s => new SectionStatusReport
                    {
                        JobId = s.JobId,
                        JobName = s.Job?.Name ?? "Unknown",
                        SectionId = s.Id,
                        SectionType = s.SectionType,
                        SectionName = s.SectionType.GetDisplayName(),
                        Status = s.Status,
                        StatusName = s.Status.GetStatusName(),
                        StartDate = s.StartDate,
                        CompletionDate = s.CompletionDate,
                        ResponsibleEmployeeId = s.ResponsibleEmployeeId,
                        ResponsibleEmployeeName = s.ResponsibleEmployee?.FirstName + " " + s.ResponsibleEmployee?.LastName ?? "Unassigned",
                        IsSubcontracted = s.IsSubcontracted,
                        SubcontractorId = s.SubcontractorId,
                        SubcontractorName = s.Subcontractor?.CompanyName ?? "None",
                        InspectionDate = s.InspectionDate,
                        ReinspectionDate = s.ReinspectionDate,
                        InspectionResult = s.InspectionResult,
                        PermitNumber = s.PermitNumber,
                        PermitIssueDate = s.PermitIssueDate,
                        PermitExpirationDate = s.PermitExpirationDate,
                        DaysInCurrentStatus = s.StartDate.HasValue 
                            ? (DateTime.UtcNow - s.StartDate.Value).Days 
                            : 0,
                        HasMaterialsOrdered = s.MaterialsOrdered,
                        HasMaterialsReceived = s.MaterialsReceived,
                        NeedsAttention = s.Status == SectionStatus.Denied || 
                                         s.Status == SectionStatus.FailedInspection || 
                                         s.Status == SectionStatus.OnHold ||
                                         (s.InspectionDate.HasValue && s.InspectionDate.Value.Date == DateTime.UtcNow.Date)
                    })
                    .OrderBy(r => r.JobName)
                    .ThenBy(r => (int)r.SectionType)
                    .ToList();

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating section status report");
                return StatusCode(500, "An error occurred while generating the section status report");
            }
        }

        // GET: api/Reports/export-excel
        [HttpGet("export-excel")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> ExportToExcel([FromQuery] string reportType, [FromQuery] int? jobId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(reportType))
                {
                    return BadRequest("Report type is required");
                }

                // Get the appropriate data based on report type
                object reportData;
                string fileName;

                switch (reportType.ToLower())
                {
                    case "job-status":
                        var jobReport = await GetJobStatusReport();
                        reportData = jobReport.Value;
                        fileName = "JobStatusReport";
                        break;
                    case "time-tracking":
                        var timeReport = await GetTimeTrackingReport(
                            startDate: DateTime.UtcNow.AddDays(-30), 
                            jobId: jobId);
                        reportData = timeReport.Value;
                        fileName = "TimeTrackingReport";
                        break;
                    case "subcontractor":
                        var subReport = await GetSubcontractorReport(jobId: jobId);
                        reportData = subReport.Value;
                        fileName = "SubcontractorReport";
                        break;
                    case "section-status":
                        var sectionReport = await GetSectionStatusReport(jobId);
                        reportData = sectionReport.Value;
                        fileName = "SectionStatusReport";
                        break;
                    default:
                        return BadRequest("Invalid report type");
                }

                // For a specific job, include job name in filename
                if (jobId.HasValue)
                {
                    var job = await _context.Jobs.FindAsync(jobId.Value);
                    if (job != null)
                    {
                        fileName += $"_{job.JobNumber}";
                    }
                }

                fileName += $"_{DateTime.Now:yyyyMMdd}.xlsx";

                // Generate Excel report
                // In a real implementation, this would create a properly formatted Excel file
                // For this example, we'll use the basic OfficeIntegrationService method
                var jobs = jobId.HasValue 
                    ? await _context.Jobs.Where(j => j.Id == jobId.Value).ToListAsync()
                    : await _context.Jobs.ToListAsync();
                
                var excelData = await _officeService.GenerateExcelReport(jobs);
                
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report to Excel");
                return StatusCode(500, "An error occurred while exporting the report to Excel");
            }
        }

        // POST: api/Reports/create-google-report
        [HttpPost("create-google-report")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> CreateGoogleReport([FromBody] GoogleReportRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ReportType))
                {
                    return BadRequest("Report type is required");
                }

                // Get job if specified
                Job? job = null;
                if (request.JobId.HasValue)
                {
                    job = await _context.Jobs
                        .Include(j => j.Sections)
                        .Include(j => j.Assignments)
                            .ThenInclude(a => a.Employee)
                        .FirstOrDefaultAsync(j => j.Id == request.JobId.Value);
                    
                    if (job == null)
                    {
                        return NotFound("Job not found");
                    }
                }

                // Create Google Doc
                string docId = job != null 
                    ? await _googleService.CreateGoogleDocument(job)
                    : "mock-doc-id"; // This would need to be implemented properly

                return Ok(new { documentId = docId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Google report");
                return StatusCode(500, "An error occurred while creating the Google report");
            }
        }
    }

    public class JobStatusReport
    {
        public int JobId { get; set; }
        public string JobNumber { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? TargetCompletionDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        public int TotalSections { get; set; }
        public int CompletedSections { get; set; }
        public int InProgressSections { get; set; }
        public int NotStartedSections { get; set; }
        public int ProblemSections { get; set; }
        public double CompletionPercentage { get; set; }
        public int DaysRunning { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class TimeTrackingReport
    {
        public DateTime Date { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int? JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public int EntryCount { get; set; }
        public DateTime FirstClockIn { get; set; }
        public DateTime? LastClockOut { get; set; }
    }

    public class SubcontractorReport
    {
        public int SubcontractorId { get; set; }
        public string SubcontractorName { get; set; } = string.Empty;
        public int JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public int SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        public decimal ContractAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime? InspectionDate { get; set; }
        public string? InspectionResult { get; set; }
        public bool IsCompleted { get; set; }
        public int DaysRunning { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class SectionStatusReport
    {
        public int JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public int SectionId { get; set; }
        public SectionType SectionType { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public SectionStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int? ResponsibleEmployeeId { get; set; }
        public string ResponsibleEmployeeName { get; set; } = string.Empty;
        public bool IsSubcontracted { get; set; }
        public int? SubcontractorId { get; set; }
        public string SubcontractorName { get; set; } = string.Empty;
        public DateTime? InspectionDate { get; set; }
        public DateTime? ReinspectionDate { get; set; }
        public string? InspectionResult { get; set; }
        public string? PermitNumber { get; set; }
        public DateTime? PermitIssueDate { get; set; }
        public DateTime? PermitExpirationDate { get; set; }
        public int DaysInCurrentStatus { get; set; }
        public bool HasMaterialsOrdered { get; set; }
        public bool HasMaterialsReceived { get; set; }
        public bool NeedsAttention { get; set; }
    }

    public class GoogleReportRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public int? JobId { get; set; }
    }
}
