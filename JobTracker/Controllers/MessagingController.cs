using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Data;
using JobTracker.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagingController : ControllerBase
    {
        private readonly ILogger<MessagingController> _logger;
        private readonly IMessagingService _messagingService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IAIMessagingService _aiMessagingService;
        private readonly ITenantContext _tenantContext;
        private readonly JobTrackerContext _context;

        public MessagingController(
            ILogger<MessagingController> logger,
            IMessagingService messagingService,
            IWhatsAppService whatsAppService,
            IAIMessagingService aiMessagingService,
            ITenantContext tenantContext,
            JobTrackerContext context)
        {
            _logger = logger;
            _messagingService = messagingService;
            _whatsAppService = whatsAppService;
            _aiMessagingService = aiMessagingService;
            _tenantContext = tenantContext;
            _context = context;
        }

        [HttpPost("send-sms")]
        public async Task<IActionResult> SendSms([FromBody] SendSmsRequest request)
        {
            try
            {
                RequireTenant();

                var success = await _messagingService.SendSmsAsync(request.PhoneNumber, request.Message);
                if (success)
                {
                    return Ok(new { message = "SMS sent successfully" });
                }
                return BadRequest(new { message = "Failed to send SMS" });
            }
            catch (Exception ex)
            {
                if (ex is TenantContextException)
                    return Unauthorized(new { message = "Authenticated company context is required" });

                _logger.LogError(ex, "Error sending SMS");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("send-whatsapp")]
        public async Task<IActionResult> SendWhatsApp([FromBody] SendWhatsAppRequest request)
        {
            try
            {
                RequireTenant();

                var success = await _whatsAppService.SendWhatsAppMessageAsync(request.PhoneNumber, request.Message);
                if (success)
                {
                    return Ok(new { message = "WhatsApp message sent successfully" });
                }
                return BadRequest(new { message = "Failed to send WhatsApp message" });
            }
            catch (Exception ex)
            {
                if (ex is TenantContextException)
                    return Unauthorized(new { message = "Authenticated company context is required" });

                _logger.LogError(ex, "Error sending WhatsApp message");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("whatsapp-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> WhatsAppWebhook([FromBody] JsonElement payload)
        {
            try
            {
                _logger.LogInformation($"Received WhatsApp webhook: {payload}");

                if (payload.TryGetProperty("entry", out var entry) && entry.GetArrayLength() > 0)
                {
                    var firstEntry = entry[0];
                    if (firstEntry.TryGetProperty("changes", out var changes) && changes.GetArrayLength() > 0)
                    {
                        var change = changes[0];
                        if (change.TryGetProperty("value", out var value) &&
                            value.TryGetProperty("messages", out var messages) && messages.GetArrayLength() > 0)
                        {
                            var message = messages[0];
                            var from = message.GetProperty("from").GetString();
                            var text = message.GetProperty("text").GetProperty("body").GetString();

                            if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(text))
                            {
                                // Webhooks do not carry an authenticated tenant. The service
                                // therefore fails closed and will not run AI or retrieve data.
                                await _whatsAppService.ProcessIncomingMessageAsync(from, text);
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WhatsApp webhook");
                return Ok(); // Return OK to prevent webhook retries
            }
        }

        [HttpGet("whatsapp-webhook")]
        [AllowAnonymous]
        public IActionResult VerifyWhatsAppWebhook([FromQuery] string hub_mode, [FromQuery] string hub_verify_token, [FromQuery] string hub_challenge)
        {
            try
            {
                var whatsAppService = _whatsAppService as WhatsAppService;
                if (whatsAppService?.VerifyWebhook(hub_mode, hub_verify_token, hub_challenge) == true)
                {
                    return Ok(hub_challenge);
                }
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying WhatsApp webhook");
                return Unauthorized();
            }
        }

        [HttpPost("ai-query")]
        public async Task<IActionResult> ProcessAIQuery([FromBody] AIQueryRequest request)
        {
            try
            {
                RequireTenant();
                var response = await _aiMessagingService.ProcessIncomingQueryAsync(request.Query, request.JobId);
                return Ok(new { response });
            }
            catch (Exception ex)
            {
                if (ex is TenantContextException)
                    return Unauthorized(new { message = "Authenticated company context is required" });
                if (ex is CrossTenantReferenceException)
                    return Forbid();

                _logger.LogError(ex, "Error processing AI query");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("generate-message")]
        public async Task<IActionResult> GenerateMessage([FromBody] GenerateMessageRequest request)
        {
            try
            {
                RequireTenant();

                string message = request.Type switch
                {
                    "verification" => await _aiMessagingService.GenerateVerificationMessageAsync(request.EmployeeName ?? "", request.VerificationCode ?? ""),
                    "job_update" => request.Job != null ? await _aiMessagingService.GenerateJobUpdateMessageAsync(request.Job, request.UpdateType ?? "") : "Invalid job data",
                    "schedule_reminder" => request.Job != null && request.ScheduledTime.HasValue 
                        ? await _aiMessagingService.GenerateScheduleReminderAsync(request.EmployeeName ?? "", request.Job, request.ScheduledTime.Value)
                        : "Invalid schedule data",
                    _ => "Unknown message type"
                };

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                if (ex is TenantContextException)
                    return Unauthorized(new { message = "Authenticated company context is required" });
                if (ex is CrossTenantReferenceException)
                    return Forbid();

                _logger.LogError(ex, "Error generating message");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private int RequireTenant() => _tenantContext.GetCurrentCompanyId();
    }

    public class SendSmsRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class SendWhatsAppRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class AIQueryRequest
    {
        public string Query { get; set; } = string.Empty;
        public int? JobId { get; set; }
    }

    public class GenerateMessageRequest
    {
        public string Type { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string? VerificationCode { get; set; }
        public JobTracker.Models.Job? Job { get; set; }
        public string? UpdateType { get; set; }
        public DateTime? ScheduledTime { get; set; }
    }
}