using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.AspNetCore.Authorization;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,MasterAdmin")]
    public class WorkSchedulingController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<WorkSchedulingController> _logger;

        public WorkSchedulingController(JobTrackerContext context, ILogger<WorkSchedulingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("calendar/{year}/{month}")]
        public async Task<ActionResult> GetMonthlySchedule(int year, int month)
        {
            try
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var schedules = await _context.WorkSchedules
                    .Include(s => s.User)
                    .Include(s => s.Job)
                    .Where(s => s.ScheduledDate >= startDate && s.ScheduledDate <= endDate)
                    .OrderBy(s => s.ScheduledDate)
                    .ToListAsync();

                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly schedule for {Year}-{Month}", year, month);
                return StatusCode(500, "Error retrieving schedule");
            }
        }

        [HttpPost("assign")]
        public async Task<ActionResult> AssignWorkerToJob([FromBody] WorkAssignmentRequest request)
        {
            try
            {
                var schedule = new WorkSchedule
                {
                    UserId = request.UserId,
                    JobId = request.JobId,
                    ScheduledDate = request.ScheduledDate,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Status = WorkScheduleStatus.Planned,
                    Notes = request.Notes,
                    CreatedByUserId = GetCurrentUserId(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.WorkSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Worker assigned successfully", scheduleId = schedule.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning worker to job");
                return StatusCode(500, "Error creating assignment");
            }
        }

        [HttpPost("submit-for-approval")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> SubmitScheduleForApproval([FromBody] ScheduleApprovalRequest request)
        {
            try
            {
                var schedules = await _context.WorkSchedules
                    .Where(s => request.ScheduleIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var schedule in schedules)
                {
                    schedule.Status = WorkScheduleStatus.PendingApproval;
                    schedule.SubmittedForApprovalAt = DateTime.UtcNow;
                    schedule.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Schedule submitted for approval" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting schedule for approval");
                return StatusCode(500, "Error submitting for approval");
            }
        }

        [HttpPost("approve")]
        [Authorize(Roles = "MasterAdmin")]
        public async Task<ActionResult> ApproveSchedule([FromBody] ScheduleApprovalResponse response)
        {
            try
            {
                var schedules = await _context.WorkSchedules
                    .Include(s => s.User)
                    .Where(s => response.ScheduleIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var schedule in schedules)
                {
                    schedule.Status = response.Approved ? WorkScheduleStatus.Approved : WorkScheduleStatus.Rejected;
                    schedule.ApprovedByUserId = GetCurrentUserId();
                    schedule.ApprovedAt = DateTime.UtcNow;
                    schedule.ApprovalNotes = response.Notes;
                    schedule.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Send notifications to field operators
                if (response.Approved)
                {
                    await SendScheduleNotifications(schedules);
                }

                return Ok(new { message = response.Approved ? "Schedule approved" : "Schedule rejected" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing schedule approval");
                return StatusCode(500, "Error processing approval");
            }
        }

        private async Task SendScheduleNotifications(List<WorkSchedule> schedules)
        {
            // Implementation for sending SMS/email notifications to field workers
            // This would integrate with your existing notification services
        }

        private int GetCurrentUserId()
        {
            // Extract user ID from JWT token or authentication context
            return 1; // Placeholder - implement proper user context extraction
        }
    }

    public class WorkAssignmentRequest
    {
        public int UserId { get; set; }
        public int JobId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Notes { get; set; }
    }

    public class ScheduleApprovalRequest
    {
        public List<int> ScheduleIds { get; set; }
    }

    public class ScheduleApprovalResponse
    {
        public List<int> ScheduleIds { get; set; }
        public bool Approved { get; set; }
        public string Notes { get; set; }
    }

    public enum WorkScheduleStatus
    {
        Planned = 0,
        PendingApproval = 1,
        Approved = 2,
        Rejected = 3,
        InProgress = 4,
        Completed = 5
    }
}