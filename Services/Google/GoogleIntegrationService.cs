using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Docs.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using JobTrackerApp.Models;

namespace JobTrackerApp.Services.Google
{
    public class GoogleIntegrationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleIntegrationService> _logger;
        private readonly IWebHostEnvironment _environment;

        public GoogleIntegrationService(
            IConfiguration configuration,
            ILogger<GoogleIntegrationService> logger,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        private async Task<UserCredential> GetUserCredential()
        {
            string clientId = _configuration["Google:ClientId"] ?? 
                throw new InvalidOperationException("Google ClientId not configured");
            string clientSecret = _configuration["Google:ClientSecret"] ?? 
                throw new InvalidOperationException("Google ClientSecret not configured");

            // Define the scopes
            string[] scopes = { 
                CalendarService.Scope.CalendarEvents, 
                DocsService.Scope.Documents 
            };

            // Load client secrets
            var clientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            // Token file store location
            string tokenFolder = Path.Combine(_environment.ContentRootPath, "App_Data", "GoogleTokens");
            Directory.CreateDirectory(tokenFolder);

            // Get user credential
            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(tokenFolder, true));

            return credential;
        }

        public async Task<CalendarService> GetCalendarService()
        {
            try
            {
                var credential = await GetUserCredential();

                // Create Calendar service
                var service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Job Tracker App"
                });

                return service;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Google Calendar service");
                throw;
            }
        }

        public async Task<DocsService> GetDocsService()
        {
            try
            {
                var credential = await GetUserCredential();

                // Create Docs service
                var service = new DocsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Job Tracker App"
                });

                return service;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Google Docs service");
                throw;
            }
        }

        public async Task<string> CreateCalendarEvent(Job job, JobSection section)
        {
            try
            {
                if (!section.InspectionDate.HasValue)
                {
                    throw new ArgumentException("Inspection date is required to create a calendar event.");
                }

                var calendarService = await GetCalendarService();

                // Create event
                Event newEvent = new Event()
                {
                    Summary = $"Inspection: {job.Name} - {section.SectionType.GetDisplayName()}",
                    Location = job.Location,
                    Description = $"Job: {job.Name}\nSection: {section.SectionType.GetDisplayName()}\nNotes: {section.AdditionalNotes}",
                    Start = new EventDateTime()
                    {
                        DateTime = section.InspectionDate.Value,
                        TimeZone = "America/New_York", // Adjust timezone as needed
                    },
                    End = new EventDateTime()
                    {
                        DateTime = section.InspectionDate.Value.AddHours(1), // Assuming 1 hour inspection duration
                        TimeZone = "America/New_York", // Adjust timezone as needed
                    },
                    Attendees = new List<EventAttendee>()
                    {
                        new EventAttendee() { Email = "inspector@example.com" } // Placeholder
                    },
                    Reminders = new Event.RemindersData()
                    {
                        UseDefault = false,
                        Overrides = new List<EventReminder>()
                        {
                            new EventReminder() { Method = "email", Minutes = 24 * 60 }, // 1 day email reminder
                            new EventReminder() { Method = "popup", Minutes = 60 } // 1 hour popup reminder
                        }
                    }
                };

                string calendarId = "primary"; // Use primary calendar
                Event createdEvent = await calendarService.Events.Insert(newEvent, calendarId).ExecuteAsync();

                _logger.LogInformation("Created calendar event for job {JobId} section {SectionType}: {EventId}", 
                    job.Id, section.SectionType, createdEvent.Id);

                return createdEvent.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create calendar event for job {JobId} section {SectionType}", 
                    job.Id, section.SectionType);
                throw;
            }
        }

        public async Task<string> CreateGoogleDocument(Job job)
        {
            try
            {
                var docsService = await GetDocsService();

                // Create a new document
                Document doc = new Document()
                {
                    Title = $"Job Report: {job.Name}"
                };

                // Create the document
                Document createdDoc = await docsService.Documents.Create(doc).ExecuteAsync();

                // Now we would add content to the document, but this requires multiple API calls
                // and is quite complex for this example. Here's a simplified version:

                var requests = new List<Google.Apis.Docs.v1.Data.Request>()
                {
                    new Google.Apis.Docs.v1.Data.Request()
                    {
                        InsertText = new Google.Apis.Docs.v1.Data.InsertTextRequest()
                        {
                            Text = $"Job Report: {job.Name}\n\n" +
                                   $"Job Number: {job.JobNumber}\n" +
                                   $"Location: {job.Location}\n" +
                                   $"Client: {job.ClientName}\n" +
                                   $"Start Date: {job.StartDate:d}\n" +
                                   $"Status: {job.Status}\n\n" +
                                   "Job Sections:\n",
                            EndOfSegmentLocation = new Google.Apis.Docs.v1.Data.EndOfSegmentLocation()
                        }
                    }
                };

                // Add sections information
                foreach (var section in job.Sections)
                {
                    requests.Add(new Google.Apis.Docs.v1.Data.Request()
                    {
                        InsertText = new Google.Apis.Docs.v1.Data.InsertTextRequest()
                        {
                            Text = $"- {section.SectionType.GetDisplayName()}: {section.Status.GetStatusName()}\n",
                            EndOfSegmentLocation = new Google.Apis.Docs.v1.Data.EndOfSegmentLocation()
                        }
                    });
                }

                // Send the batch update request
                var batchUpdateRequest = new Google.Apis.Docs.v1.Data.BatchUpdateDocumentRequest()
                {
                    Requests = requests
                };

                await docsService.Documents.BatchUpdate(batchUpdateRequest, createdDoc.DocumentId).ExecuteAsync();

                _logger.LogInformation("Created Google document for job {JobId}: {DocumentId}", job.Id, createdDoc.DocumentId);

                return createdDoc.DocumentId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Google document for job {JobId}", job.Id);
                throw;
            }
        }
    }
}
