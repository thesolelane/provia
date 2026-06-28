using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using System.Security.Claims;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<TasksController> _logger;

        public TasksController(JobTrackerContext context, ITenantContext tenantContext, ILogger<TasksController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        private string GetCurrentUserName() =>
            User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTasks(
            [FromQuery] string? status,
            [FromQuery] string? view,      // "mine" | "all" | "overdue"
            [FromQuery] int? jobId,
            [FromQuery] int? contactId,
            [FromQuery] int? leadId)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var userId = GetCurrentUserId();
                var now = DateTime.UtcNow;

                var query = _context.TaskItems
                    .Where(t => t.CompanyId == companyId && t.Status != "cancelled");

                if (view == "mine" && userId.HasValue)
                    query = query.Where(t => t.AssignedToUserId == userId);
                else if (view == "overdue")
                    query = query.Where(t => t.DueDate < now && t.Status != "complete");

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(t => t.Status == status);

                if (jobId.HasValue) query = query.Where(t => t.JobId == jobId);
                if (contactId.HasValue) query = query.Where(t => t.ContactId == contactId);
                if (leadId.HasValue) query = query.Where(t => t.LeadId == leadId);

                var tasks = await query
                    .OrderBy(t => t.Status == "complete" ? 1 : 0)
                    .ThenBy(t => t.DueDate == null ? 1 : 0)
                    .ThenBy(t => t.DueDate)
                    .ThenByDescending(t => t.Priority == "urgent" ? 4 : t.Priority == "high" ? 3 : t.Priority == "medium" ? 2 : 1)
                    .Select(t => new
                    {
                        t.Id, t.Title, t.Description, t.DueDate,
                        t.AssignedToUserId, t.AssignedToName,
                        t.JobId, t.ContactId, t.LeadId,
                        t.Status, t.Priority,
                        t.IsRecurring, t.RecurrenceRule,
                        t.ParentTaskId, t.CompletedAt, t.CompletedBy,
                        t.CreatedAt,
                        IsOverdue = t.DueDate < now && t.Status != "complete"
                    })
                    .ToListAsync();

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tasks");
                return StatusCode(500, new { message = "Error fetching tasks" });
            }
        }

        // GET: api/tasks/counts
        [HttpGet("counts")]
        public async Task<ActionResult<object>> GetCounts()
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var userId = GetCurrentUserId();
                var now = DateTime.UtcNow;

                var all = await _context.TaskItems
                    .Where(t => t.CompanyId == companyId && t.Status != "cancelled" && t.Status != "complete")
                    .ToListAsync();

                return Ok(new
                {
                    Total = all.Count,
                    Mine = userId.HasValue ? all.Count(t => t.AssignedToUserId == userId) : 0,
                    Overdue = all.Count(t => t.DueDate.HasValue && t.DueDate < now),
                    DueToday = all.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date == now.Date)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task counts");
                return StatusCode(500, new { message = "Error fetching counts" });
            }
        }

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTask(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var task = await _context.TaskItems
                    .Where(t => t.Id == id && t.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                if (task == null) return NotFound(new { message = "Task not found" });
                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task {Id}", id);
                return StatusCode(500, new { message = "Error fetching task" });
            }
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<object>> CreateTask([FromBody] CreateTaskRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();

                var task = new TaskItem
                {
                    CompanyId = companyId,
                    Title = req.Title.Trim(),
                    Description = req.Description?.Trim(),
                    DueDate = req.DueDate,
                    AssignedToUserId = req.AssignedToUserId,
                    AssignedToName = req.AssignedToName?.Trim(),
                    JobId = req.JobId,
                    ContactId = req.ContactId,
                    LeadId = req.LeadId,
                    Status = "pending",
                    Priority = req.Priority ?? "medium",
                    IsRecurring = req.IsRecurring,
                    RecurrenceRule = req.IsRecurring ? req.RecurrenceRule : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TaskItems.Add(task);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetTask), new { id = task.Id }, new { task.Id, task.Title });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, new { message = "Error creating task" });
            }
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] CreateTaskRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId);
                if (task == null) return NotFound(new { message = "Task not found" });

                task.Title = req.Title?.Trim() ?? task.Title;
                task.Description = req.Description?.Trim();
                task.DueDate = req.DueDate;
                task.AssignedToUserId = req.AssignedToUserId;
                task.AssignedToName = req.AssignedToName?.Trim();
                task.JobId = req.JobId;
                task.ContactId = req.ContactId;
                task.LeadId = req.LeadId;
                task.Priority = req.Priority ?? task.Priority;
                task.IsRecurring = req.IsRecurring;
                task.RecurrenceRule = req.IsRecurring ? req.RecurrenceRule : null;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {Id}", id);
                return StatusCode(500, new { message = "Error updating task" });
            }
        }

        // POST: api/tasks/{id}/complete
        [HttpPost("{id}/complete")]
        public async Task<ActionResult<object>> CompleteTask(int id, [FromBody] CompleteTaskRequest? req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId);
                if (task == null) return NotFound(new { message = "Task not found" });

                task.Status = "complete";
                task.CompletedAt = DateTime.UtcNow;
                task.CompletedBy = GetCurrentUserName();
                task.CompletionNote = req?.Note?.Trim();
                task.UpdatedAt = DateTime.UtcNow;

                int? nextTaskId = null;

                // Spawn next occurrence for recurring tasks
                if (task.IsRecurring && !string.IsNullOrWhiteSpace(task.RecurrenceRule))
                {
                    var baseDue = task.DueDate ?? DateTime.UtcNow;
                    var nextDue = RecurrenceRules.NextDue(baseDue, task.RecurrenceRule);

                    var nextTask = new TaskItem
                    {
                        CompanyId = companyId,
                        Title = task.Title,
                        Description = task.Description,
                        DueDate = nextDue,
                        AssignedToUserId = task.AssignedToUserId,
                        AssignedToName = task.AssignedToName,
                        JobId = task.JobId,
                        ContactId = task.ContactId,
                        LeadId = task.LeadId,
                        Status = "pending",
                        Priority = task.Priority,
                        IsRecurring = true,
                        RecurrenceRule = task.RecurrenceRule,
                        ParentTaskId = task.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.TaskItems.Add(nextTask);
                    await _context.SaveChangesAsync();
                    nextTaskId = nextTask.Id;
                }
                else
                {
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Task completed", nextTaskId, isRecurring = task.IsRecurring });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task {Id}", id);
                return StatusCode(500, new { message = "Error completing task" });
            }
        }

        // POST: api/tasks/{id}/status
        [HttpPost("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] TaskStatusRequest req)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId);
                if (task == null) return NotFound(new { message = "Task not found" });

                task.Status = req.Status;
                task.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Status updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task status {Id}", id);
                return StatusCode(500, new { message = "Error updating status" });
            }
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId);
                if (task == null) return NotFound(new { message = "Task not found" });

                task.Status = "cancelled";
                task.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {Id}", id);
                return StatusCode(500, new { message = "Error deleting task" });
            }
        }
    }

    public class CreateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public int? AssignedToUserId { get; set; }
        public string? AssignedToName { get; set; }
        public int? JobId { get; set; }
        public int? ContactId { get; set; }
        public int? LeadId { get; set; }
        public string? Priority { get; set; }
        public bool IsRecurring { get; set; } = false;
        public string? RecurrenceRule { get; set; }
    }

    public class CompleteTaskRequest
    {
        public string? Note { get; set; }
    }

    public class TaskStatusRequest
    {
        public string Status { get; set; } = "pending";
    }
}
