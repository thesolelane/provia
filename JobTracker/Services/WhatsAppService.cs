using System.Text;
using System.Text.Json;

namespace JobTracker.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly IAIMessagingService _aiMessagingService;
        private readonly ITenantContext _tenantContext;
        private readonly string? _accessToken;
        private readonly string? _phoneNumberId;
        private readonly string? _verifyToken;

        public WhatsAppService(HttpClient httpClient, ILogger<WhatsAppService> logger, 
            IAIMessagingService aiMessagingService, IConfiguration configuration,
            ITenantContext tenantContext)
        {
            _httpClient = httpClient;
            _logger = logger;
            _aiMessagingService = aiMessagingService;
            _tenantContext = tenantContext;
            _accessToken = configuration["WHATSAPP_ACCESS_TOKEN"];
            _phoneNumberId = configuration["WHATSAPP_PHONE_NUMBER_ID"];
            _verifyToken = configuration["WHATSAPP_VERIFY_TOKEN"];

            _httpClient.BaseAddress = new Uri("https://graph.facebook.com/v18.0/");
            
            if (!string.IsNullOrEmpty(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            }
        }

        public async Task<bool> SendWhatsAppMessageAsync(string phoneNumber, string message)
        {
            _tenantContext.GetCurrentCompanyId();

            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_phoneNumberId))
            {
                _logger.LogWarning("WhatsApp credentials not configured. Cannot send message.");
                return false;
            }

            try
            {
                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "text",
                    text = new { body = message }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_phoneNumberId}/messages", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"WhatsApp message sent successfully to {phoneNumber}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send WhatsApp message to {phoneNumber}: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while sending WhatsApp message to {phoneNumber}");
                return false;
            }
        }

        public async Task<string> ProcessIncomingMessageAsync(string fromNumber, string messageContent)
        {
            try
            {
                // A provider webhook has no authenticated tenant in this application.
                // Refuse to run retrieval or AI rather than guessing a company from
                // the sender, payload, or message content.
                _tenantContext.GetCurrentCompanyId();

                _logger.LogInformation($"Processing incoming WhatsApp message from {fromNumber}: {messageContent}");

                // Generate AI response
                var aiResponse = await _aiMessagingService.GenerateResponseToWhatsAppMessageAsync(fromNumber, messageContent);

                // Send response back via WhatsApp
                await SendWhatsAppMessageAsync(fromNumber, aiResponse);

                return aiResponse;
            }
            catch (TenantContextException)
            {
                _logger.LogWarning("Ignored WhatsApp AI message because no authenticated tenant context was available.");
                return "This channel is not configured for an authenticated company.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing incoming WhatsApp message from {fromNumber}");
                var errorMessage = "I'm experiencing technical difficulties. Please contact your supervisor for assistance.";
                await SendWhatsAppMessageAsync(fromNumber, errorMessage);
                return errorMessage;
            }
        }

        public async Task<bool> SetupWebhookAsync(string webhookUrl)
        {
            try
            {
                _logger.LogInformation($"Setting up WhatsApp webhook for URL: {webhookUrl}");
                
                // WhatsApp webhook setup is typically done through the Meta Developer Console
                // This method can be used for webhook validation
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up WhatsApp webhook");
                return false;
            }
        }

        public bool VerifyWebhook(string mode, string token, string challenge)
        {
            if (mode == "subscribe" && token == _verifyToken)
            {
                _logger.LogInformation("WhatsApp webhook verified successfully");
                return true;
            }
            
            _logger.LogWarning("WhatsApp webhook verification failed");
            return false;
        }
    }
}