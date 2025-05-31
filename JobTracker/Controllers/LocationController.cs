using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly ILogger<LocationController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string? _googleApiKey;

        public LocationController(ILogger<LocationController> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            _googleApiKey = configuration["GOOGLE_API_KEY"] ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        }

        [HttpPost("google-geolocation")]
        public async Task<IActionResult> GetGoogleGeolocation([FromBody] GoogleGeolocationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(_googleApiKey))
                {
                    return BadRequest(new { error = "Google API key not configured" });
                }

                var googleRequest = new
                {
                    considerIp = request.ConsiderIp,
                    wifiAccessPoints = request.WifiAccessPoints,
                    cellTowers = request.CellTowers
                };

                var json = JsonSerializer.Serialize(googleRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"https://www.googleapis.com/geolocation/v1/geolocate?key={_googleApiKey}",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<GoogleGeolocationResponse>(responseContent);
                    
                    if (result?.Location != null)
                    {
                        return Ok(new
                        {
                            location = new
                            {
                                lat = result.Location.Lat,
                                lng = result.Location.Lng,
                                accuracy = result.Accuracy
                            }
                        });
                    }
                    else
                    {
                        return BadRequest(new { message = "Invalid location data received from Google API" });
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Google Geolocation API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return BadRequest(new { error = "Google Geolocation API request failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Google Geolocation API");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    public class GoogleGeolocationRequest
    {
        public bool ConsiderIp { get; set; }
        public WifiAccessPoint[] WifiAccessPoints { get; set; } = Array.Empty<WifiAccessPoint>();
        public CellTower[] CellTowers { get; set; } = Array.Empty<CellTower>();
    }

    public class WifiAccessPoint
    {
        public string MacAddress { get; set; }
        public int SignalStrength { get; set; }
        public string Age { get; set; }
    }

    public class CellTower
    {
        public int CellId { get; set; }
        public int LocationAreaCode { get; set; }
        public int MobileCountryCode { get; set; }
        public int MobileNetworkCode { get; set; }
        public int Age { get; set; }
        public int SignalStrength { get; set; }
    }

    public class GoogleGeolocationResponse
    {
        public LocationData Location { get; set; }
        public double Accuracy { get; set; }
    }

    public class LocationData
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}