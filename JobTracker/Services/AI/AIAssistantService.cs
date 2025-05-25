using System.Text.Json;

namespace JobTracker.Services.AI
{
    public class AIAssistantService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIAssistantService> _logger;
        private readonly string? _openAiApiKey;

        public AIAssistantService(HttpClient httpClient, ILogger<AIAssistantService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        public async Task<string> GetFireBlockingGuidance(string sectionType)
        {
            var fireBlockingKnowledge = GetFireBlockingRequirements();
            
            if (string.IsNullOrEmpty(_openAiApiKey))
            {
                // Provide local fire blocking guidance if no API key
                return GetLocalFireBlockingGuidance(sectionType);
            }

            try
            {
                var prompt = $@"
                As a Massachusetts building code expert, provide specific fire blocking guidance for {sectionType} work.

                Fire Blocking Requirements from Massachusetts Building Code:
                {fireBlockingKnowledge}

                Please provide:
                1. Specific fire blocking requirements for {sectionType}
                2. Materials that can be used (foam, lumber, fire-resistant materials)
                3. Critical inspection points
                4. Common violations to avoid

                Focus on practical, actionable guidance for contractors.
                ";

                return await CallOpenAI(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI fire blocking guidance");
                return GetLocalFireBlockingGuidance(sectionType);
            }
        }

        private string GetFireBlockingRequirements()
        {
            return @"
            MASSACHUSETTS BUILDING CODE - FIRE BLOCKING REQUIREMENTS:

            1. GENERAL REQUIREMENTS (Section 718):
            - Fire blocking required in concealed spaces of combustible construction
            - Required at ceiling and floor levels
            - Required at connections between vertical and horizontal spaces

            2. FLOOR/CEILING ASSEMBLIES:
            - Fire blocking required at every floor level
            - Block all interconnected concealed vertical and horizontal spaces
            - Include spaces created by dropped ceilings, raised floors

            3. STAIRWAY FIRE BLOCKING:
            - Fire blocking required under stairs
            - Block space between stringers at top and bottom
            - Enclose usable space under stairs with proper fire rating

            4. PENETRATIONS THROUGH FIRE BLOCKING:
            - Electrical: Seal all holes through plates with approved materials
            - Plumbing: Seal floor penetrations with fire-resistant materials
            - HVAC: Seal duct penetrations according to code requirements
            - Use only approved fire-blocking materials

            5. APPROVED FIRE BLOCKING MATERIALS:
            - 2-inch nominal lumber
            - Two thicknesses of 1-inch nominal lumber with joints offset
            - One thickness of 0.719-inch wood structural panel
            - One thickness of 0.75-inch particleboard
            - One thickness of 1/2-inch gypsum board
            - One thickness of 1/4-inch cement board
            - Batts or blankets of mineral wool or glass fiber
            - Expandable foam plastics (UL listed for fire blocking)

            6. CRITICAL INSPECTION POINTS:
            - Verify materials meet code requirements
            - Check proper installation at all required locations
            - Ensure penetrations are properly sealed
            - Confirm fire blocking is in place before covering
            ";
        }

        private string GetLocalFireBlockingGuidance(string sectionType)
        {
            return sectionType.ToLower() switch
            {
                "framing" => @"
                FRAMING FIRE BLOCKING REQUIREMENTS:
                ✓ Install fire blocking between studs at ceiling level
                ✓ Block all horizontal spaces created by framing
                ✓ Use 2x lumber, 1/2"" gypsum board, or approved materials
                ✓ CRITICAL: Must be inspected before insulation/drywall
                
                Common Materials:
                - 2-inch nominal lumber (most common)
                - 1/2-inch gypsum board
                - Mineral wool batts
                - UL-listed expanding foam
                ",
                
                "electrical" => @"
                ELECTRICAL ROUGH-IN FIRE BLOCKING:
                ✓ Seal all holes drilled through top/bottom plates
                ✓ Use fire-rated caulk or expanding foam for small holes
                ✓ Install proper fire blocking around large penetrations
                ✓ CRITICAL: Inspect before insulation installation
                
                Penetration Sealing:
                - Holes < 1"": Fire-rated caulk or expanding foam
                - Holes > 1"": Fire blocking materials + sealant
                - Multiple cables: Use fire-rated putty pads
                ",
                
                "plumbing" => @"
                PLUMBING ROUGH-IN FIRE BLOCKING:
                ✓ Seal all floor penetrations where pipes pass through
                ✓ Use fire-resistant materials around pipe chases
                ✓ Install proper blocking in wall cavities with plumbing
                ✓ CRITICAL: Inspect before covering pipes
                
                Floor Penetration Requirements:
                - Seal gaps around pipes with fire-rated materials
                - Use fire-blocking foam for small gaps
                - Install proper fire stopping for large openings
                - Maintain fire rating of floor assembly
                ",
                
                _ => @"
                GENERAL FIRE BLOCKING REQUIREMENTS:
                ✓ Required before covering any concealed spaces
                ✓ Use approved materials (lumber, gypsum, mineral wool, foam)
                ✓ Seal all penetrations through fire blocking
                ✓ Schedule inspection before proceeding to next phase
                "
            };
        }

        private async Task<string> CallOpenAI(string prompt)
        {
            var request = new
            {
                model = "gpt-4o", // the newest OpenAI model is "gpt-4o" which was released May 13, 2024. do not change this unless explicitly requested by the user
                messages = new[]
                {
                    new { role = "system", content = "You are a Massachusetts building code expert specializing in fire safety and inspection requirements." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 800,
                temperature = 0.3
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"OpenAI API error: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var aiResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            
            return aiResponse.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Unable to get fire blocking guidance.";
        }
    }
}