using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SmsTestController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SmsTestController> _logger;

        public SmsTestController(HttpClient httpClient, ILogger<SmsTestController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendTestSms([FromBody] SmsRequest request)
        {
            var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
            var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
            var fromNumber = Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER");

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
            {
                return BadRequest(new { message = "Twilio credentials not configured" });
            }

            try
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                var payload = new Dictionary<string, string>
                {
                    {"From", fromNumber},
                    {"To", request.PhoneNumber},
                    {"Body", request.Message}
                };

                var encodedContent = new FormUrlEncodedContent(payload);
                var apiUrl = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

                var response = await _httpClient.PostAsync(apiUrl, encodedContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"SMS sent successfully to {request.PhoneNumber}");
                    return Ok(new { 
                        message = "SMS sent successfully",
                        twilioResponse = responseContent 
                    });
                }
                else
                {
                    _logger.LogError($"Failed to send SMS: {response.StatusCode} - {responseContent}");
                    return BadRequest(new { 
                        message = "Failed to send SMS",
                        error = responseContent 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending SMS");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }

    public class SmsRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}