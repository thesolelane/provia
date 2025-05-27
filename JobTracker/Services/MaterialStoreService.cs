using JobTracker.Models;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Services
{
    public class MaterialStoreService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<MaterialStoreService> _logger;
        
        // Main office location: 37 Duckmill Rd Fitchburg MA 01420
        private const double OFFICE_LATITUDE = 42.5617;
        private const double OFFICE_LONGITUDE = -71.8028;
        private const double MAX_DISTANCE_MILES = 35.0;

        public MaterialStoreService(JobTrackerContext context, ILogger<MaterialStoreService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedMaterialStoresAsync()
        {
            try
            {
                // Check if stores already exist
                if (await _context.MaterialStores.AnyAsync())
                {
                    _logger.LogInformation("Material stores already exist in database");
                    return;
                }

                var stores = GetFitchburgAreaStores();
                
                await _context.MaterialStores.AddRangeAsync(stores);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Added {Count} material stores to database", stores.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding material stores");
            }
        }

        private List<MaterialStore> GetFitchburgAreaStores()
        {
            return new List<MaterialStore>
            {
                // Home Depot locations
                new MaterialStore
                {
                    StoreName = "The Home Depot #4619",
                    StoreType = "Home Improvement",
                    Address = "344 John Fitch Hwy",
                    City = "Fitchburg",
                    State = "MA",
                    ZipCode = "01420",
                    PhoneNumber = "(978) 345-0204",
                    Latitude = 42.5534,
                    Longitude = -71.8203,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5534, -71.8203),
                    Hours = "Mon-Sat: 6:00 AM - 10:00 PM, Sun: 8:00 AM - 8:00 PM"
                },
                new MaterialStore
                {
                    StoreName = "The Home Depot #4618",
                    StoreType = "Home Improvement",
                    Address = "20 Timpany Blvd",
                    City = "Gardner",
                    State = "MA",
                    ZipCode = "01440",
                    PhoneNumber = "(978) 632-2900",
                    Latitude = 42.5751,
                    Longitude = -72.0095,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5751, -72.0095),
                    Hours = "Mon-Sat: 6:00 AM - 10:00 PM, Sun: 8:00 AM - 8:00 PM"
                },

                // Lowe's locations
                new MaterialStore
                {
                    StoreName = "Lowe's Home Improvement",
                    StoreType = "Home Improvement",
                    Address = "353 John Fitch Hwy",
                    City = "Fitchburg",
                    State = "MA",
                    ZipCode = "01420",
                    PhoneNumber = "(978) 345-1913",
                    Latitude = 42.5542,
                    Longitude = -71.8211,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5542, -71.8211),
                    Hours = "Mon-Sat: 6:00 AM - 10:00 PM, Sun: 8:00 AM - 8:00 PM"
                },

                // Local lumber yards
                new MaterialStore
                {
                    StoreName = "Wachusett Lumber Co",
                    StoreType = "Lumber",
                    Address = "45 Boutelle St",
                    City = "Fitchburg",
                    State = "MA",
                    ZipCode = "01420",
                    PhoneNumber = "(978) 342-3029",
                    Latitude = 42.5833,
                    Longitude = -71.8028,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5833, -71.8028),
                    Hours = "Mon-Fri: 7:00 AM - 4:30 PM, Sat: 7:00 AM - 3:00 PM"
                },
                new MaterialStore
                {
                    StoreName = "Taylor Lumber & Hardware",
                    StoreType = "Lumber & Hardware",
                    Address = "9 Crescent St",
                    City = "Athol",
                    State = "MA",
                    ZipCode = "01331",
                    PhoneNumber = "(978) 249-3436",
                    Latitude = 42.5967,
                    Longitude = -72.2267,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5967, -72.2267),
                    Hours = "Mon-Fri: 7:00 AM - 5:00 PM, Sat: 7:00 AM - 4:00 PM"
                },

                // Electrical supply stores
                new MaterialStore
                {
                    StoreName = "Rexel USA (Platt Electric)",
                    StoreType = "Electrical",
                    Address = "12 Commercial Dr",
                    City = "Leominster",
                    State = "MA",
                    ZipCode = "01453",
                    PhoneNumber = "(978) 537-1881",
                    Latitude = 42.5251,
                    Longitude = -71.7598,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5251, -71.7598),
                    Hours = "Mon-Fri: 7:00 AM - 5:00 PM, Sat: 8:00 AM - 12:00 PM"
                },

                // Plumbing supply stores
                new MaterialStore
                {
                    StoreName = "Ferguson Plumbing Supply",
                    StoreType = "Plumbing",
                    Address = "150 Commercial Rd",
                    City = "Leominster",
                    State = "MA",
                    ZipCode = "01453",
                    PhoneNumber = "(978) 534-6346",
                    Latitude = 42.5186,
                    Longitude = -71.7895,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5186, -71.7895),
                    Hours = "Mon-Fri: 7:00 AM - 5:00 PM, Sat: 7:30 AM - 12:00 PM"
                },
                new MaterialStore
                {
                    StoreName = "Hajoca Corporation",
                    StoreType = "Plumbing",
                    Address = "25 Erdman Way",
                    City = "Leominster",
                    State = "MA",
                    ZipCode = "01453",
                    PhoneNumber = "(978) 840-9881",
                    Latitude = 42.5298,
                    Longitude = -71.7456,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5298, -71.7456),
                    Hours = "Mon-Fri: 7:00 AM - 4:30 PM"
                },

                // Hardware stores
                new MaterialStore
                {
                    StoreName = "Aubuchon Hardware",
                    StoreType = "Hardware",
                    Address = "344 Main St",
                    City = "Fitchburg",
                    State = "MA",
                    ZipCode = "01420",
                    PhoneNumber = "(978) 342-4100",
                    Latitude = 42.5834,
                    Longitude = -71.8028,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5834, -71.8028),
                    Hours = "Mon-Sat: 8:00 AM - 8:00 PM, Sun: 9:00 AM - 6:00 PM"
                },
                new MaterialStore
                {
                    StoreName = "Ace Hardware of Leominster",
                    StoreType = "Hardware",
                    Address = "742 Merriam Ave",
                    City = "Leominster",
                    State = "MA",
                    ZipCode = "01453",
                    PhoneNumber = "(978) 537-2717",
                    Latitude = 42.5098,
                    Longitude = -71.7598,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5098, -71.7598),
                    Hours = "Mon-Sat: 8:00 AM - 8:00 PM, Sun: 9:00 AM - 6:00 PM"
                },

                // Building supply stores in nearby towns
                new MaterialStore
                {
                    StoreName = "Capitol Lumber",
                    StoreType = "Lumber",
                    Address = "145 Erdman Way",
                    City = "Leominster",
                    State = "MA",
                    ZipCode = "01453",
                    PhoneNumber = "(978) 537-1410",
                    Latitude = 42.5345,
                    Longitude = -71.7412,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5345, -71.7412),
                    Hours = "Mon-Fri: 7:00 AM - 5:00 PM, Sat: 7:00 AM - 4:00 PM"
                },

                // Specialized stores
                new MaterialStore
                {
                    StoreName = "Granite State Glass",
                    StoreType = "Glass & Windows",
                    Address = "978 Central St",
                    City = "Leominster",
                    State = "MA",
                    ZipCode = "01453",
                    PhoneNumber = "(978) 534-6222",
                    Latitude = 42.5187,
                    Longitude = -71.7698,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5187, -71.7698),
                    Hours = "Mon-Fri: 7:30 AM - 4:30 PM"
                },
                new MaterialStore
                {
                    StoreName = "Blackstone Millwork",
                    StoreType = "Millwork",
                    Address = "126 Lancaster St",
                    City = "Leominster",
                    State = "MA",
                    ZipCode = "01453",
                    PhoneNumber = "(978) 537-2142",
                    Latitude = 42.5234,
                    Longitude = -71.7543,
                    DistanceFromOffice = CalculateDistance(OFFICE_LATITUDE, OFFICE_LONGITUDE, 42.5234, -71.7543),
                    Hours = "Mon-Fri: 7:00 AM - 4:00 PM"
                }
            };
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusMiles = 3959;

            var lat1Rad = DegreesToRadians(lat1);
            var lat2Rad = DegreesToRadians(lat2);
            var deltaLatRad = DegreesToRadians(lat2 - lat1);
            var deltaLonRad = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusMiles * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public async Task<List<MaterialStore>> GetNearbyStoresAsync(double latitude, double longitude, double maxDistanceMiles = 35.0)
        {
            return await _context.MaterialStores
                .Where(s => s.IsActive)
                .OrderBy(s => s.DistanceFromOffice)
                .ToListAsync();
        }

        public async Task<List<MaterialStore>> GetStoresByTypeAsync(string storeType)
        {
            return await _context.MaterialStores
                .Where(s => s.IsActive && s.StoreType.ToLower().Contains(storeType.ToLower()))
                .OrderBy(s => s.DistanceFromOffice)
                .ToListAsync();
        }
    }
}