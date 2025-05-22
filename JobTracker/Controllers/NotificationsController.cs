using Microsoft.AspNetCore.Mvc;
using JobTracker.Services;
using System.Threading.Tasks;
using System;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // POST: api/Notifications/whatsapp
        [HttpPost("whatsapp")]
        public async Task<IActionResult> SendWhatsAppNotification([FromBody] WhatsAppNotificationRequest request)
        {
            if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Phone number and message are required");
            }

            try
            {
                var result = await _notificationService.SendWhatsAppNotification(request.PhoneNumber, request.Message);
                if (result)
                {
                    return Ok(new { Success = true, Message = "WhatsApp notification sent successfully" });
                }
                else
                {
                    return StatusCode(500, new { Success = false, Message = "Failed to send WhatsApp notification" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp notification");
                return StatusCode(500, new { Success = false, Message = "An error occurred while sending the WhatsApp notification" });
            }
        }
    }

    public class WhatsAppNotificationRequest
    {
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
    }
}