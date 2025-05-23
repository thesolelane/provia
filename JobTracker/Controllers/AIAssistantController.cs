using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIAssistantController : ControllerBase
    {
        private readonly ILogger<AIAssistantController> _logger;

        public AIAssistantController(ILogger<AIAssistantController> logger)
        {
            _logger = logger;
        }

        [HttpPost("building-code-assistance")]
        public IActionResult GetBuildingCodeAssistance([FromBody] AIAssistanceRequest request)
        {
            try
            {
                var response = GetAIResponse(request.Query);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting building code assistance");
                return Ok(new { 
                    Response = "I can help with building codes, permits, and construction guidance! Try asking about electrical permits or plumbing requirements.", 
                    Confidence = 0.8f, 
                    Sources = new[] { "Construction Knowledge Base" } 
                });
            }
        }

        private object GetAIResponse(string query)
        {
            query = query.ToLower();

            if ((query.Contains("deck") || query.Contains("decking")) && (query.Contains("fastener") || query.Contains("fastners") || query.Contains("screw") || query.Contains("bolt")))
            {
                return new {
                    Response = @"**DECK FASTENER REQUIREMENTS - Field Reference**

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

**MA Building Code (10th Edition - 780 CMR):**
• All fasteners must resist corrosion (galvanized/stainless)
• Structural connections require load-rated hardware
• Spacing per IRC R507 (2021 ICC standards)
• Effective October 2024, concurrency period until June 2025

**Safety:** Pre-drill holes, use proper PPE, check load ratings.",
                    Confidence = 0.95f,
                    Sources = new[] { "Massachusetts Building Code", "IRC R507", "Construction Standards" }
                };
            }
            else if (query.Contains("electrical") || query.Contains("electric"))
            {
                return new {
                    Response = @"Electrical Work Requirements in Massachusetts:

• Licensed electrician required for all new circuits and panel work
• Permits needed from local building department
• GFCI protection required in bathrooms, kitchens, outdoor outlets
• AFCI breakers required for bedroom circuits
• All work must meet current NEC standards
• Inspection required before covering wiring

Safety Tip: Never attempt electrical work without proper licensing!",
                    Confidence = 0.85f,
                    Sources = new[] { "Massachusetts Building Code", "NEC Standards" }
                };
            }
            else if (query.Contains("plumbing") || query.Contains("pipe"))
            {
                return new {
                    Response = @"Plumbing Code Requirements:

• Licensed plumber required for new installations
• Permits needed for fixture changes and new lines
• Proper venting required for all drains
• Backflow prevention may be required
• Water pressure must meet minimum standards
• All connections must be accessible for inspection

Common Requirements:
- 3-inch minimum for toilet drains
- Proper slope for drain lines (1/4 inch per foot)
- Hot water lines on left, cold on right",
                    Confidence = 0.85f,
                    Sources = new[] { "Massachusetts Plumbing Code" }
                };
            }
            else if (query.Contains("foundation") || query.Contains("basement"))
            {
                return new {
                    Response = @"Foundation Requirements in Massachusetts:

• Minimum 4-foot depth for frost protection
• Proper drainage and waterproofing mandatory
• Structural engineer approval for major changes
• Soil testing recommended for new foundations
• Vapor barriers required in basements
• Proper insulation to prevent freezing

Key Considerations:
- New England frost line depth varies by location
- Drainage is critical to prevent water damage
- Building permits required for foundation work",
                    Confidence = 0.80f,
                    Sources = new[] { "Massachusetts Building Code", "Frost Protection Guidelines" }
                };
            }
            else if (query.Contains("permit") || query.Contains("inspection"))
            {
                return new {
                    Response = @"Building Permits and Inspections:

Typically Required For:
• Electrical work (new circuits, panels)
• Plumbing changes (fixtures, lines)
• Structural modifications
• HVAC installations
• Major renovations

Inspection Schedule:
• Rough-in inspections (before covering work)
• Final inspections (project completion)
• Some work requires multiple inspections

Pro Tip: Always check with your local building department first - requirements vary by municipality!",
                    Confidence = 0.90f,
                    Sources = new[] { "Local Building Department Guidelines" }
                };
            }
            else if (query.Contains("timeline") || query.Contains("schedule"))
            {
                return new {
                    Response = @"Typical Construction Timelines:

Based on Industry Standards:
• Demolition: 1-3 days
• Foundation work: 3-7 days  
• Framing: 3-10 days
• Electrical rough-in: 2-5 days
• Plumbing rough-in: 2-5 days
• HVAC installation: 2-4 days
• Drywall: 3-7 days
• Painting: 2-5 days
• Flooring: 2-6 days
• Final trim/finishing: 2-4 days

Planning Tips:
- Add 20-30% buffer time for delays
- Weather can affect exterior work
- Material delivery delays are common
- Coordinate inspections in advance",
                    Confidence = 0.75f,
                    Sources = new[] { "Historical Project Data" }
                };
            }
            else if (query.Contains("budget") || query.Contains("cost"))
            {
                return new {
                    Response = @"Construction Budget Planning:

Typical Cost Breakdown:
• Materials: 40-50% of total budget
• Labor: 30-40% of total budget  
• Permits/fees: 2-5% of total budget
• Contingency: 10-20% of total budget

Cost Control Tips:
• Get multiple contractor quotes
• Order materials early to avoid rush charges
• Consider seasonal pricing variations
• Plan for unexpected discoveries
• Keep receipts for warranty claims

Massachusetts Considerations:
- Higher labor costs in Boston area
- Winter work may cost more
- Local permit fees vary significantly",
                    Confidence = 0.70f,
                    Sources = new[] { "Industry Cost Analysis" }
                };
            }
            else if (query.Contains("year") || query.Contains("built") || query.Contains("age") || query.Contains("old"))
            {
                return new {
                    Response = @"Building Age and Code Considerations:

**Pre-1978 Buildings:**
• Lead paint concerns - special handling required
• Asbestos may be present in insulation and tiles
• Electrical systems may need upgrading to current codes
• Plumbing may use galvanized or lead pipes

**Pre-1960 Buildings:**
• Knob-and-tube wiring common - replacement often required
• Foundation waterproofing may be inadequate
• Insulation typically below current standards
• HVAC systems likely need modernization

**Modern Buildings (Post-2000):**
• Generally meet current energy efficiency standards
• May have newer electrical panels and GFCI protection
• Proper insulation and vapor barriers

**Key Inspections for Older Buildings:**
• Structural integrity assessment
• Electrical safety evaluation
• Lead and asbestos testing
• Foundation and drainage review
• HVAC efficiency analysis

**Massachusetts Requirements:**
- Lead paint disclosure required for pre-1978 properties
- Asbestos regulations strictly enforced
- Energy efficiency upgrades may be required for major renovations",
                    Confidence = 0.85f,
                    Sources = new[] { "Massachusetts Building Code", "EPA Guidelines", "Historic Building Standards" }
                };
            }
            else
            {
                return new {
                    Response = "I can help you with building codes, permits, timelines, and budgets! Try asking about electrical requirements, plumbing codes, or project scheduling. I'm here to make your construction projects smoother and code-compliant!",
                    Confidence = 0.60f,
                    Sources = new[] { "General Construction Knowledge" }
                };
            }
        }
    }

    public class AIAssistanceRequest
    {
        public string Query { get; set; } = string.Empty;
        public string? SectionType { get; set; }
    }
}