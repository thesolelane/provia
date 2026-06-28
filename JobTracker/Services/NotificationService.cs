using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobTracker.Services
{
    public enum WhatsAppProvider
    {
        Twilio,
        Dialog360
    }
    
    public interface INotificationService
    {
        Task<bool> SendWhatsAppMessage(string to, string message, WhatsAppProvider provider = WhatsAppProvider.Dialog360);
    }

    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        
        // 360dialog credentials
        private readonly string? _dialog360ApiKey;
        private readonly string? _dialog360PhoneNumber;
        
        // Twilio credentials
        private readonly string? _twilioAccountSid;
        private readonly string? _twilioAuthToken;
        private readonly string? _twilioPhoneNumber;

        public NotificationService(ILogger<NotificationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
            
            // Get 360dialog credentials from environment variables
            _dialog360ApiKey = Environment.GetEnvironmentVariable("DIALOG360_API_KEY");
            _dialog360PhoneNumber = Environment.GetEnvironmentVariable("DIALOG360_PHONE_NUMBER");
            
            // Get Twilio credentials from environment variables
            _twilioAccountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
            _twilioAuthToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
            _twilioPhoneNumber = Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER");
        }

        public async Task<bool> SendWhatsAppMessage(string to, string message, WhatsAppProvider provider = WhatsAppProvider.Dialog360)
        {
            try
            {
                // Format phone number for WhatsApp
                var toFormatted = to.Trim();
                if (!toFormatted.StartsWith("+"))
                {
                    // Add + if it's missing
                    toFormatted = "+" + toFormatted;
                }
                
                // Choose provider based on parameter
                if (provider == WhatsAppProvider.Dialog360)
                {
                    return await SendViaDialog360(toFormatted, message);
                }
                else
                {
                    return await SendViaTwilio(toFormatted, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending WhatsApp message: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> SendViaDialog360(string to, string message)
        {
            // Check if 360dialog is configured
            if (string.IsNullOrEmpty(_dialog360ApiKey) || string.IsNullOrEmpty(_dialog360PhoneNumber))
            {
                _logger.LogError("360dialog credentials not configured");
                return false;
            }
            
            // Create the request payload
            var payload = new
            {
                to = to,
                type = "text",
                text = new
                {
                    body = message
                }
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");
            
            // Set the authorization header
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("D360-API-KEY", _dialog360ApiKey);
            
            // Make the API call to 360dialog
            var response = await _httpClient.PostAsync(
                $"https://waba.360dialog.io/v1/messages",
                content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"WhatsApp message sent successfully via 360dialog: {responseContent}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to send WhatsApp message via 360dialog. Status: {response.StatusCode}, Error: {errorContent}");
                return false;
            }
        }
        
        private async Task<bool> SendViaTwilio(string to, string message)
        {
            // Check if Twilio is configured
            if (string.IsNullOrEmpty(_twilioAccountSid) || string.IsNullOrEmpty(_twilioAuthToken) || string.IsNullOrEmpty(_twilioPhoneNumber))
            {
                _logger.LogError("Twilio credentials not configured");
                return false;
            }
            
            // Format WhatsApp numbers
            var fromWhatsApp = $"whatsapp:{_twilioPhoneNumber}";
            var toWhatsApp = $"whatsapp:{to}";
            
            // Create the request payload
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("From", fromWhatsApp),
                new KeyValuePair<string, string>("To", toWhatsApp),
                new KeyValuePair<string, string>("Body", message)
            });
            
            // Set the authorization header (Basic Auth for Twilio)
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_twilioAccountSid}:{_twilioAuthToken}"));
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            
            // Make the API call to Twilio
            var response = await _httpClient.PostAsync(
                $"https://api.twilio.com/2010-04-01/Accounts/{_twilioAccountSid}/Messages.json",
                formContent);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"WhatsApp message sent successfully via Twilio: {responseContent}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to send WhatsApp message via Twilio. Status: {response.StatusCode}, Error: {errorContent}");
                return false;
            }
        }
    }
}