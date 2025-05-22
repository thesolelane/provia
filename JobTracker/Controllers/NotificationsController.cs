using System.Threading.Tasks;
using JobTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly ILogger<NotificationsController> _logger;
        private readonly INotificationService _notificationService;

        public NotificationsController(ILogger<NotificationsController> logger, INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        public class WhatsAppMessageRequest
        {
            public string? PhoneNumber { get; set; }
            public string? Message { get; set; }
            public string? Provider { get; set; } = "Dialog360"; // Default provider
        }

        [HttpPost("whatsapp")]
        public async Task<IActionResult> SendWhatsAppMessage([FromBody] WhatsAppMessageRequest request)
        {
            if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { success = false, message = "Phone number and message are required" });
            }

            // Determine which provider to use
            WhatsAppProvider provider = WhatsAppProvider.Dialog360; // Default
            if (!string.IsNullOrEmpty(request.Provider))
            {
                if (request.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
                {
                    provider = WhatsAppProvider.Twilio;
                }
            }

            _logger.LogInformation($"Sending WhatsApp message to {request.PhoneNumber} using {provider} provider");
            var result = await _notificationService.SendWhatsAppMessage(request.PhoneNumber!, request.Message!, provider);

            if (result)
            {
                return Ok(new { success = true, message = $"Message sent successfully via {provider}" });
            }
            else
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = $"Failed to send message via {provider}. Check the logs for details." 
                });
            }
        }
    }
}