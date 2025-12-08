using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JobTracker.Models;
using Microsoft.Extensions.Logging;

namespace JobTracker.Services
{
    public interface IMassGISService
    {
        Task<MassGisPropertyData?> GetPropertyDataByAddressAsync(string address, string city);
        Task<MassGisPropertyData?> GetPropertyDataByParcelIdAsync(string parcelId, string town);
        Task<MassGisPropertyData?> GetPropertyDataByCoordinatesAsync(double latitude, double longitude);
    }

    public class MassGISService : IMassGISService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MassGISService> _logger;

        private const string MASSGIS_FEATURE_SERVICE_URL = 
            "https://services1.arcgis.com/hGdibHYSPO59RG1h/arcgis/rest/services/L3_AGGREGATE_PAR_ASSESS/FeatureServer/0/query";

        public MassGISService(HttpClient httpClient, ILogger<MassGISService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<MassGisPropertyData?> GetPropertyDataByAddressAsync(string address, string city)
        {
            try
            {
                var cleanAddress = address.Replace("'", "''").Trim().ToUpper();
                var cleanCity = city.Replace("'", "''").Trim().ToUpper();

                var whereClause = $"UPPER(SITE_ADDR) LIKE '%{cleanAddress}%' AND UPPER(TOWN) = '{cleanCity}'";

                return await QueryMassGISAsync(whereClause);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching property data by address: {Address}, {City}", address, city);
                return null;
            }
        }

        public async Task<MassGisPropertyData?> GetPropertyDataByParcelIdAsync(string parcelId, string town)
        {
            try
            {
                var cleanParcelId = parcelId.Replace("'", "''").Trim();
                var cleanTown = town.Replace("'", "''").Trim().ToUpper();

                var whereClause = $"MAP_PAR = '{cleanParcelId}' AND UPPER(TOWN) = '{cleanTown}'";

                return await QueryMassGISAsync(whereClause);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching property data by parcel ID: {ParcelId}, {Town}", parcelId, town);
                return null;
            }
        }

        public async Task<MassGisPropertyData?> GetPropertyDataByCoordinatesAsync(double latitude, double longitude)
        {
            try
            {
                var url = $"{MASSGIS_FEATURE_SERVICE_URL}?" +
                    $"geometry={longitude},{latitude}&" +
                    "geometryType=esriGeometryPoint&" +
                    "inSR=4326&" +
                    "spatialRel=esriSpatialRelIntersects&" +
                    "outFields=*&" +
                    "returnGeometry=true&" +
                    "f=json";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return ParseGISResponse(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching property data by coordinates: {Lat}, {Long}", latitude, longitude);
                return null;
            }
        }

        private async Task<MassGisPropertyData?> QueryMassGISAsync(string whereClause)
        {
            try
            {
                var url = $"{MASSGIS_FEATURE_SERVICE_URL}?" +
                    $"where={Uri.EscapeDataString(whereClause)}&" +
                    "outFields=*&" +
                    "returnGeometry=true&" +
                    "resultRecordCount=1&" +
                    "f=json";

                _logger.LogInformation("Querying MassGIS: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return ParseGISResponse(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying MassGIS with where clause: {Where}", whereClause);
                return null;
            }
        }

        private MassGisPropertyData? ParseGISResponse(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("features", out var features) || features.GetArrayLength() == 0)
                {
                    _logger.LogWarning("No features found in MassGIS response");
                    return null;
                }

                var feature = features[0];
                var attributes = feature.GetProperty("attributes");

                var data = new MassGisPropertyData
                {
                    LocId = GetStringProperty(attributes, "LOC_ID"),
                    Town = GetStringProperty(attributes, "TOWN"),
                    SiteAddress = GetStringProperty(attributes, "SITE_ADDR"),
                    Owner = GetStringProperty(attributes, "OWNER1"),
                    OwnerAddress = BuildOwnerAddress(attributes),
                    MapPar = GetStringProperty(attributes, "MAP_PAR"),
                    UseCode = GetStringProperty(attributes, "USE_CODE"),
                    UseDescription = GetStringProperty(attributes, "USE_DESC"),
                    LotSize = GetDecimalProperty(attributes, "LOT_SIZE"),
                    LandValue = GetDecimalProperty(attributes, "LAND_VAL"),
                    BuildingValue = GetDecimalProperty(attributes, "BLDG_VAL"),
                    TotalValue = GetDecimalProperty(attributes, "TOTAL_VAL"),
                    YearBuilt = GetIntProperty(attributes, "YEAR_BUILT"),
                    FiscalYear = GetIntProperty(attributes, "FY")
                };

                if (feature.TryGetProperty("geometry", out var geometry))
                {
                    if (geometry.TryGetProperty("x", out var x) && geometry.TryGetProperty("y", out var y))
                    {
                        data.Longitude = x.GetDouble();
                        data.Latitude = y.GetDouble();
                    }
                }

                _logger.LogInformation("Successfully parsed property data for: {Address}, {Town}", 
                    data.SiteAddress, data.Town);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing MassGIS response");
                return null;
            }
        }

        private string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
            {
                return prop.GetString();
            }
            return null;
        }

        private decimal? GetDecimalProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetDecimal();
            }
            return null;
        }

        private int? GetIntProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetInt32();
            }
            return null;
        }

        private string? BuildOwnerAddress(JsonElement attributes)
        {
            var parts = new List<string>();

            var ownerAddr = GetStringProperty(attributes, "OWN_ADDR");
            var ownerCity = GetStringProperty(attributes, "OWN_CITY");
            var ownerState = GetStringProperty(attributes, "OWN_STATE");
            var ownerZip = GetStringProperty(attributes, "OWN_ZIP");

            if (!string.IsNullOrEmpty(ownerAddr)) parts.Add(ownerAddr);
            if (!string.IsNullOrEmpty(ownerCity)) parts.Add(ownerCity);
            if (!string.IsNullOrEmpty(ownerState)) parts.Add(ownerState);
            if (!string.IsNullOrEmpty(ownerZip)) parts.Add(ownerZip);

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }
    }
}
