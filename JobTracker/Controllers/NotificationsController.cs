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
            public string PhoneNumber { get; set; }
            public string Message { get; set; }
        }

        [HttpPost("whatsapp")]
        public async Task<IActionResult> SendWhatsAppMessage([FromBody] WhatsAppMessageRequest request)
        {
            if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { success = false, message = "Phone number and message are required" });
            }

            _logger.LogInformation($"Sending WhatsApp message to {request.PhoneNumber}");
            var result = await _notificationService.SendWhatsAppMessage(request.PhoneNumber, request.Message);

            if (result)
            {
                return Ok(new { success = true, message = "Message sent successfully" });
            }
            else
            {
                return StatusCode(500, new { success = false, message = "Failed to send message. Check the logs for details." });
            }
        }
    }
}