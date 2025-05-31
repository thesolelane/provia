using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Security;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/schedules")]
    public class AdminScheduleController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<AdminScheduleController> _logger;
        private readonly ISecurityAuditService _auditService;

        public AdminScheduleController(JobTrackerContext context, ILogger<AdminScheduleController> logger, ISecurityAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        [HttpGet("{year}/{month}")]
        public async Task<IActionResult> GetSchedule(int year, int month)
        {
            try
            {
                await _auditService.LogSecurityEventAsync("SCHEDULE_ACCESS", null, $"Year: {year}, Month: {month}", GetClientIP());
                
                var schedules = await _context.WorkSchedules
                    .Where(s => s.ScheduledDate.Year == year && s.ScheduledDate.Month == month)
                    .Include(s => s.User)
                    .Include(s => s.Job)
                    .ToListAsync();

                var scheduleData = schedules.GroupBy(s => s.ScheduledDate.ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.Select(s => new
                    {
                        id = s.Id,
                        workerId = s.UserId,
                        workerName = s.User.FirstName + " " + s.User.LastName,
                        jobId = s.JobId,
                        jobName = s.Job.Name,
                        status = s.Status == 1 ? "pending" : s.Status == 2 ? "approved" : "rejected",
                        date = s.ScheduledDate.ToString("yyyy-MM-dd"),
                        startTime = s.StartTime?.ToString(),
                        endTime = s.EndTime?.ToString(),
                        notes = s.Notes
                    }).ToList());

                return Ok(new
                {
                    scheduleData = scheduleData,
                    isSubmittedForApproval = schedules.Any(s => s.Status == 1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schedule for {Year}/{Month}", year, month);
                return StatusCode(500, "Error retrieving schedule");
            }
        }

        [HttpPost("submit-for-approval")]
        public async Task<IActionResult> SubmitForApproval([FromBody] ScheduleSubmissionRequest request)
        {
            try
            {
                await _auditService.LogSecurityEventAsync("SCHEDULE_SUBMISSION", null, 
                    $"Month: {request.Month}, Year: {request.Year}", GetClientIP());

                foreach (var dateSchedules in request.ScheduleData)
                {
                    foreach (var assignment in dateSchedules.Value)
                    {
                        var schedule = new WorkSchedule
                        {
                            UserId = assignment.WorkerId,
                            JobId = assignment.JobId,
                            ScheduledDate = DateTime.Parse(dateSchedules.Key),
                            Status = 1, // Pending approval
                            Notes = assignment.Notes,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.WorkSchedules.Add(schedule);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Schedule submitted for approval" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting schedule for approval");
                return StatusCode(500, "Error submitting schedule");
            }
        }

        private string GetClientIP()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public class ScheduleSubmissionRequest
    {
        public Dictionary<string, List<ScheduleAssignment>> ScheduleData { get; set; } = new();
        public int Month { get; set; }
        public int Year { get; set; }
    }

    public class ScheduleAssignment
    {
        public int WorkerId { get; set; }
        public int JobId { get; set; }
        public string? Notes { get; set; }
    }

    public class WorkSchedule
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int JobId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int Status { get; set; } // 1=Pending, 2=Approved, 3=Rejected
        public string? Notes { get; set; }
        public string? ApprovalNotes { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? ApprovedByUserId { get; set; }
        public DateTime? SubmittedForApprovalAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public Job Job { get; set; } = null!;
    }
}