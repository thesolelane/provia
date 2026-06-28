using System.Text;
using System.Text.Json;
using JobTrackerApp.Models;
using JobTrackerApp.Services.BuildingCode;

namespace JobTrackerApp.Services.AI
{
    public class AIAssistantService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIAssistantService> _logger;
        private readonly HttpClient _httpClient;
        private readonly BuildingCodeService _buildingCodeService;

        public AIAssistantService(
            IConfiguration configuration,
            ILogger<AIAssistantService> logger,
            HttpClient httpClient,
            BuildingCodeService buildingCodeService)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _buildingCodeService = buildingCodeService;

            // Configure HttpClient for OpenAI
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _configuration["OpenAI:APIKey"];
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task<string> GetBuildingCodeAssistance(string query, SectionType? sectionType = null)
        {
            try
            {
                // Get related building codes if section type is provided
                string relevantCodes = "";
                if (sectionType.HasValue)
                {
                    var sectionCodes = await _buildingCodeService.GetCodesBySection(sectionType.Value);
                    relevantCodes = string.Join("\n", sectionCodes.Select(c => 
                        $"Code {c.CodeNumber}: {c.Title}\n{c.Description}\n"));
                }

                // Prepare the prompt with context
                string systemPrompt = @"You are an expert construction assistant specialized in building codes, materials, and best practices for construction and renovation projects. 
Provide specific, detailed answers about:
- Building materials and fasteners (screws, bolts, nails, anchors)
- Construction techniques and methods
- Building code requirements and compliance
- Permit processes and inspection criteria
- Safety regulations and best practices
- Material specifications and installation guidelines

Give practical, actionable advice with specific product recommendations, measurements, and step-by-step guidance when appropriate.
Focus on Massachusetts building codes when relevant, but also provide general construction knowledge.";

                if (!string.IsNullOrEmpty(relevantCodes))
                {
                    systemPrompt += "\n\nHere are some relevant building codes that may apply to this question:\n" + relevantCodes;
                }

                // Prepare the API request
                var requestData = new
                {
                    model = _configuration["OpenAI:Model"] ?? "gpt-4",
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = query }
                    },
                    temperature = float.Parse(_configuration["OpenAI:Temperature"] ?? "0.7"),
                    max_tokens = int.Parse(_configuration["OpenAI:MaxTokens"] ?? "2000")
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                // Make the API request
                var response = await _httpClient.PostAsync("chat/completions", content);
                response.EnsureSuccessStatusCode();

                // Process the response
                var responseJson = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseJson);
                
                string assistantResponse = responseObject
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Sorry, I couldn't generate a response at this time.";

                _logger.LogInformation("Generated AI response for building code query related to section {SectionType}", sectionType);
                
                return assistantResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting building code assistance from AI: {ErrorMessage}", ex.Message);
                
                // Return comprehensive construction knowledge for field use
                var queryLower = query.ToLower();
                
                if (queryLower.Contains("deck") && queryLower.Contains("fastener"))
                {
                    return @"**DECK FASTENER REQUIREMENTS - Field Reference**

**Joist Connections:**
• Joist hangers: Galvanized steel, sized for lumber (2x8, 2x10, 2x12)
• Joist hanger nails: 1½"" galvanized, 10d minimum
• Beam connections: ½"" galvanized carriage bolts

**Decking Attachment:**
• Deck screws: #8 x 2½"" stainless steel or galvanized
• Spacing: 12"" on center along joists
• End spacing: 1"" minimum from board ends

**Ledger Board (House Attachment):**
• Lag bolts: ½"" x 6"" galvanized, every 16"" on center
• Through bolts preferred in seismic areas
• Flashing required above ledger

**MA Building Code:**
• All fasteners must resist corrosion (galvanized/stainless)
• Structural connections require load-rated hardware
• Spacing per IRC R507

**Safety:** Pre-drill holes, use proper PPE, check load ratings.";
                }
                
                if (queryLower.Contains("electrical") && (queryLower.Contains("permit") || queryLower.Contains("code")))
                {
                    return @"**ELECTRICAL PERMITS & CODES - Field Reference**

**Permit Required For:**
• New circuits and panels
• GFCI installations
• Outdoor/basement wiring
• Hot tub/pool electrical

**MA Electrical Code (527 CMR 12.00):**
• GFCI required: bathrooms, kitchens, outdoor, basement
• AFCI required: bedrooms, living areas (NEC 210.12)
• Dedicated 20A circuits: kitchen appliances

**Common Requirements:**
• Boxes: Accessible, proper fill calculation
• Wire size: 14 AWG = 15A max, 12 AWG = 20A max
• Grounding: Required on all circuits

**Inspection:** Rough-in before closing walls, final after completion.";
                }
                
                if (queryLower.Contains("plumbing") && (queryLower.Contains("code") || queryLower.Contains("permit")))
                {
                    return @"**PLUMBING CODES & PERMITS - Field Reference**

**MA Plumbing Code (248 CMR):**
• Permits required for new fixtures, water lines, gas lines
• Licensed plumber required for gas work

**Common Requirements:**
• Water supply: ¾"" minimum service, ½"" branch lines
• Drainage: 3"" minimum building drain, proper slope (¼""/ft)
• Venting: Each fixture group requires proper vent

**Rough-in Standards:**
• Toilet: 12"" center to wall, 15"" minimum side clearance
• Sink: Hot on left, 8"" centers standard
• Shower: 32""x32"" minimum

**Testing:** Pressure test water lines, smoke/water test drains.";
                }
                
                if (queryLower.Contains("foundation") || queryLower.Contains("footing"))
                {
                    return @"**FOUNDATION REQUIREMENTS - Field Reference**

**MA Building Code (780 CMR):**
• Frost depth: 4 feet minimum below grade
• Footing width: Minimum 2x wall thickness
• Concrete: 3000 PSI minimum, #4 rebar required

**Excavation:**
• Call Dig Safe (811) 72 hours before digging
• Proper slope for safety (1:1 maximum)
• Drainage required around foundation

**Concrete Work:**
• Forms: Level, properly braced
• Placement: No free fall over 5 feet
• Curing: Keep moist 7 days minimum

**Inspection:** Footing/foundation before concrete pour.";
                }
                
                if (queryLower.Contains("massachusetts") && queryLower.Contains("building") && queryLower.Contains("code"))
                {
                    return @"**MASSACHUSETTS BUILDING CODE COMPREHENSIVE REFERENCE**

**PRIMARY CODES:**
• Building Code: 780 CMR (9th Edition Based on IBC 2015)
• Residential Code: 780 CMR Appendix Q (IRC 2015)
• Energy Code: 780 CMR Appendix 115.AA (IECC 2015)
• Accessibility: 521 CMR (AAB)

**ELECTRICAL (527 CMR 12.00):**
• GFCI: Required bathrooms, kitchens, outdoor, unfinished basements
• AFCI: Required bedrooms, family rooms, dining rooms, living rooms
• Smoke/CO detectors: Hardwired with battery backup required

**PLUMBING (248 CMR):**
• Water service: ¾"" minimum, lead-free materials required
• Septic systems: Title V compliance (310 CMR 15.000)
• Backflow prevention required for irrigation systems

**HVAC (780 CMR Chapter 13):**
• Permits required for new systems, ductwork modifications
• Energy efficiency: Must meet 780 CMR Appendix 115.AA
• Carbon monoxide detectors required near fuel-burning appliances

**STRUCTURAL:**
• Snow load: 30 psf minimum (varies by region)
• Wind load: 110 mph basic wind speed
• Seismic: Design category A (low seismic)

**PERMITS REQUIRED:**
• Building permits: New construction, additions >120 sq ft
• Electrical: New circuits, panel upgrades, GFCI installations
• Plumbing: New fixtures, water/sewer connections
• Mechanical: HVAC systems, fireplaces, wood stoves

**LOCAL AUTHORITIES:**
• Building Inspector: Municipal authority having jurisdiction
• Inspections: Foundation, framing, rough mechanical/electrical, final
• Appeals: Local Board of Appeals, then State Building Code Appeals Board

**KEY PHONE NUMBERS:**
• Dig Safe: 811
• State Building Code: (617) 727-7532
• Architectural Access Board: (617) 727-0660

**RESOURCES:**
• Mass.gov Building Code section
• Local building department
• Professional engineer for complex projects";
                }
                
                return "I can help with building codes, permits, timelines, and budgets! Try asking about electrical requirements, plumbing codes, or project scheduling. I'm here to make your construction projects smoother and code-compliant!";
            }
        }

        public async Task<string> GetJobSectionGuidance(JobSection section)
        {
            try
            {
                // Prepare section-specific information
                var sectionInfo = new StringBuilder();
                sectionInfo.AppendLine($"Section Type: {section.SectionType.GetDisplayName()}");
                sectionInfo.AppendLine($"Current Status: {section.Status.GetStatusName()}");
                
                if (!string.IsNullOrEmpty(section.Description))
                    sectionInfo.AppendLine($"Description: {section.Description}");
                
                if (section.InspectionDate.HasValue)
                    sectionInfo.AppendLine($"Scheduled Inspection Date: {section.InspectionDate.Value:d}");
                
                if (!string.IsNullOrEmpty(section.InspectionResult))
                    sectionInfo.AppendLine($"Last Inspection Result: {section.InspectionResult}");
                
                if (!string.IsNullOrEmpty(section.MaterialsRequired))
                    sectionInfo.AppendLine($"Materials Required: {section.MaterialsRequired}");

                // Get relevant building codes
                var sectionCodes = await _buildingCodeService.GetCodesBySection(section.SectionType);
                var relevantCodes = string.Join("\n", sectionCodes.Select(c => 
                    $"Code {c.CodeNumber}: {c.Title}\n{c.Description}\n"));

                // Prepare the prompt with context
                string systemPrompt = @"You are an expert assistant specialized in Massachusetts building codes and construction practices. 
Provide detailed guidance for the current construction job section, including required steps, compliance considerations, 
inspection preparation advice, and best practices. Focus on practical, actionable advice that helps ensure code compliance and quality work.";

                systemPrompt += "\n\nHere is information about the current job section:\n" + sectionInfo.ToString();
                
                if (!string.IsNullOrEmpty(relevantCodes))
                {
                    systemPrompt += "\n\nRelevant Massachusetts building codes:\n" + relevantCodes;
                }

                string userQuery = $"Please provide guidance on how to successfully complete the {section.SectionType.GetDisplayName()} section, including preparation for inspection and compliance with Massachusetts building codes.";

                // Prepare the API request
                var requestData = new
                {
                    model = _configuration["OpenAI:Model"] ?? "gpt-4",
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userQuery }
                    },
                    temperature = float.Parse(_configuration["OpenAI:Temperature"] ?? "0.7"),
                    max_tokens = int.Parse(_configuration["OpenAI:MaxTokens"] ?? "2000")
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                // Make the API request
                var response = await _httpClient.PostAsync("chat/completions", content);
                response.EnsureSuccessStatusCode();

                // Process the response
                var responseJson = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseJson);
                
                string assistantResponse = responseObject
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Sorry, I couldn't generate guidance at this time.";

                _logger.LogInformation("Generated AI guidance for job section {SectionType}", section.SectionType);
                
                return assistantResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job section guidance from AI for section {SectionType}", section.SectionType);
                return "I apologize, but I'm having trouble generating specific guidance for this job section right now. Please try again later or consult the Massachusetts building code reference materials directly.";
            }
        }
    }
}
