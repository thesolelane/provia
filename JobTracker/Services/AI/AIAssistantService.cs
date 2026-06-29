using System.Text.Json;

namespace JobTracker.Services.AI
{
    public class JobScopeResult
    {
        public string Description { get; set; } = string.Empty;
        public string Notes       { get; set; } = string.Empty;
    }

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

        // ── Job Scope Generator ──────────────────────────────────────────────────
        public async Task<JobScopeResult> GenerateJobScopeAsync(string jobName, string location, string clientName, decimal budget, string? tradeHint)
        {
            if (string.IsNullOrEmpty(_openAiApiKey))
                return GetLocalJobScope(jobName, location, budget, tradeHint);

            try
            {
                var prompt = $@"
You are a construction project manager writing a professional job scope for a Massachusetts contractor.

Job Name: {jobName}
Location: {location}
Client: {clientName}
Budget: ${budget:N0}
Trade/Hint: {tradeHint ?? "general construction"}

Write a concise, professional job description (2-3 sentences, max 400 characters) and a brief internal notes entry (1-2 sentences, max 200 characters).

Respond ONLY with valid JSON in this exact format, no other text:
{{
  ""description"": ""..."",
  ""notes"": ""...""
}}";

                var request = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a construction project manager. Respond only with the requested JSON, no markdown, no commentary." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 300,
                    temperature = 0.4
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

                var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request);
                if (!response.IsSuccessStatusCode)
                    return GetLocalJobScope(jobName, location, budget, tradeHint);

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var aiResponse  = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                var content     = aiResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                // Strip markdown code fences if present
                content = content.Trim().TrimStart('`');
                if (content.StartsWith("json")) content = content[4..];
                content = content.TrimEnd('`').Trim();

                var result = JsonSerializer.Deserialize<JobScopeResult>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result ?? GetLocalJobScope(jobName, location, budget, tradeHint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating job scope with AI");
                return GetLocalJobScope(jobName, location, budget, tradeHint);
            }
        }

        private static JobScopeResult GetLocalJobScope(string jobName, string location, decimal budget, string? tradeHint)
        {
            var trade = tradeHint?.ToLower() ?? "";
            var desc = trade switch
            {
                var t when t.Contains("electric") =>
                    $"Electrical work for {jobName} at {location}. Scope includes installation, wiring, panel work, and code compliance per 527 CMR. All work to meet Massachusetts electrical code requirements.",
                var t when t.Contains("plumb") =>
                    $"Plumbing installation and service for {jobName} at {location}. Scope includes rough-in, fixture installation, and pressure testing per 248 CMR. All work to meet Massachusetts plumbing code.",
                var t when t.Contains("hvac") || t.Contains("heat") =>
                    $"HVAC installation for {jobName} at {location}. Scope includes equipment installation, ductwork, and commissioning. System to meet Massachusetts energy code requirements.",
                var t when t.Contains("roof") =>
                    $"Roofing work for {jobName} at {location}. Scope includes tear-off, underlayment, new roofing system installation, and flashing. All materials to meet Massachusetts building code.",
                _ =>
                    $"Construction work for {jobName} at {location}. Scope of work includes site preparation, material procurement, installation, and final inspection. All work to comply with Massachusetts building codes and permit requirements."
            };
            var notes = $"Budget ${budget:N0}. Confirm permit requirements with local building department before start. Schedule inspections per MA 780 CMR.";
            return new JobScopeResult { Description = desc[..Math.Min(desc.Length, 490)], Notes = notes[..Math.Min(notes.Length, 190)] };
        }

        // ── Invoice Line-Item Assistant ──────────────────────────────────────────
        public async Task<string> GenerateInvoiceLineItemsAsync(string workDescription, decimal totalBudget)
        {
            if (string.IsNullOrEmpty(_openAiApiKey))
                return GetLocalInvoiceItems(workDescription, totalBudget);

            try
            {
                var prompt = $@"
You are a construction billing specialist. Generate invoice line items for this work:

Work Description: {workDescription}
Total Budget: ${totalBudget:N0}

Respond ONLY with a JSON array, no other text:
[
  {{""description"": ""Labor — Rough-in"", ""quantity"": 1, ""unitPrice"": 2500.00}},
  {{""description"": ""Materials"", ""quantity"": 1, ""unitPrice"": 800.00}}
]

Include 3-6 line items. Quantities as numbers. Prices as numbers (no $ symbol). Total should be near ${totalBudget:N0}.";

                var request = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a construction billing specialist. Respond only with the requested JSON array, no markdown." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 400,
                    temperature = 0.3
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

                var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request);
                if (!response.IsSuccessStatusCode) return GetLocalInvoiceItems(workDescription, totalBudget);

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var aiResponse  = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                var content     = aiResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                content = content.Trim().TrimStart('`');
                if (content.StartsWith("json")) content = content[4..];
                content = content.TrimEnd('`').Trim();
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice line items");
                return GetLocalInvoiceItems(workDescription, totalBudget);
            }
        }

        private static string GetLocalInvoiceItems(string description, decimal budget)
        {
            var labor    = Math.Round(budget * 0.60m, 2);
            var materials = Math.Round(budget * 0.30m, 2);
            var permit   = Math.Round(budget * 0.05m, 2);
            var cleanup  = budget - labor - materials - permit;
            return $@"[
  {{""description"": ""Labor — {description}"", ""quantity"": 1, ""unitPrice"": {labor}}},
  {{""description"": ""Materials & Supplies"", ""quantity"": 1, ""unitPrice"": {materials}}},
  {{""description"": ""Permit & Inspection Fees"", ""quantity"": 1, ""unitPrice"": {permit}}},
  {{""description"": ""Site Cleanup & Disposal"", ""quantity"": 1, ""unitPrice"": {cleanup}}}
]";
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