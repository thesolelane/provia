using Microsoft.AspNetCore.Mvc;
using JobTracker.Services;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/WorkingAuth")]
    public class SmsVerificationController : ControllerBase
    {
        private readonly IMessagingService _messagingService;
        private readonly ILogger<SmsVerificationController> _logger;

        public SmsVerificationController(IMessagingService messagingService, ILogger<SmsVerificationController> logger)
        {
            _messagingService = messagingService;
            _logger = logger;
        }

        [HttpPost("send-phone-verification")]
        public async Task<IActionResult> SendPhoneVerification([FromBody] PhoneVerificationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PhoneNumber))
                {
                    return BadRequest(new { message = "Phone number is required", success = false });
                }

                // Generate verification code
                var verificationCode = new Random().Next(100000, 999999).ToString();

                // Send SMS via Twilio
                if (_messagingService != null)
                {
                    try
                    {
                        var success = await _messagingService.SendVerificationSmsAsync(request.PhoneNumber, verificationCode);
                        
                        if (success)
                        {
                            return Ok(new { 
                                message = $"Verification SMS sent to {request.PhoneNumber}",
                                details = "Message sent via Twilio",
                                success = true
                            });
                        }
                        else
                        {
                            return Ok(new { 
                                message = "SMS service configuration issue",
                                verificationCode = verificationCode,
                                details = "Check Twilio credentials",
                                success = false
                            });
                        }
                    }
                    catch (Exception smsEx)
                    {
                        _logger.LogError(smsEx, "Error sending verification SMS");
                        return Ok(new { 
                            message = "SMS sending failed",
                            verificationCode = verificationCode,
                            details = smsEx.Message,
                            success = false
                        });
                    }
                }
                else
                {
                    return Ok(new { 
                        message = "SMS service not available",
                        verificationCode = verificationCode,
                        details = "Messaging service not configured",
                        success = false
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in phone verification process");
                return StatusCode(500, new { 
                    message = "Internal server error",
                    details = ex.Message,
                    success = false
                });
            }
        }
    }


}