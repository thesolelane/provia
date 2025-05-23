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
            else if (query.Contains("tension") || query.Contains("tie") || query.Contains("hurricane") || query.Contains("seismic") || query.Contains("connector"))
            {
                return new {
                    Response = @"**STRUCTURAL CONNECTORS & TENSION TIES**

**Hurricane/Seismic Ties:**
• Simpson Strong-Tie H1 Hurricane Ties: Common for joist-to-plate connections
• H2.5A Hurricane Ties: Heavy-duty rafter-to-plate connections
• LTP Tension Ties: Uplift resistance for roof systems
• Install with specified Simpson nails (typically 10d x 1.5"")

**Tension Ties Applications:**
• Required in high-wind areas (120+ mph zones)
• Seismic regions (Massachusetts generally low seismic)
• Connects framing members to resist uplift forces
• Joist hangers for beam connections

**Installation Requirements:**
• Use only manufacturer-specified fasteners
• Pre-drill when required to prevent splitting
• Follow spacing requirements exactly
• Check local wind load requirements

**Massachusetts Code Notes:**
• Refer to IRC Table R602.3(1) for fastening schedules
• Wind speeds: Check local jurisdiction requirements
• Some coastal areas require enhanced tie-down systems
• Building official may require engineered connections

**Common Sizes:**
- 2x lumber: H1, H2.5, H3
- Engineered lumber: Consult manufacturer
- Heavy timber: Custom fabricated ties may be required",
                    Confidence = 0.90f,
                    Sources = new[] { "Massachusetts Building Code", "IRC R602", "Simpson Strong-Tie Standards" }
                };
            }
            else if (query.Contains("nail") || query.Contains("fastener") || query.Contains("screw") || query.Contains("stud") || query.Contains("joist") || query.Contains("rafter"))
            {
                return new {
                    Response = @"**MASSACHUSETTS FASTENING SCHEDULE (780 CMR 120.Q)**

**FRAMING CONNECTIONS:**
• Stud to sole plate: 8d common (4 toe-nail) or 16d common (2 direct-nail)
• Stud to cap plate: 16d common (2 toe-nail or 2 direct-nail)
• Double studs: 10d common @ 12"" o.c. direct
• Corner studs: 16d common @ 24"" o.c. direct
• Sole plate to joist/blocking: 16d common @ 16"" o.c.

**ROOF FRAMING:**
• Roof rafter to plate: 8d common (3 toe-nail)
• Roof rafter to ridge: 16d common (2 toe-nail or direct nail)
• Jack rafter to hip: 10d or 16d common (3 toe-nail or 2 direct-nail)

**FLOOR FRAMING:**
• Floor joists to studs (no ceiling joists): 10d common (5 direct or 3 direct)
• Floor joists to studs (with ceiling joists): 10d common (2 direct)
• Floor joists to sill or girder: 3d common (3 toe-nail)
• Ceiling joists to plate: 16d common (3 toe-nail)

**SHEATHING & SUBFLOORING:**
• Plywood roof/wall (½"" or less): 6d common @ 6"" o.c. edges, 12"" o.c. field
• Plywood roof/wall (⅝"" or greater): 8d common @ 6"" o.c. edges, 12"" o.c. field
• Plywood subflooring (¾""): 8d common @ 6"" o.c. edges, 10"" o.c. field
• 1"" subflooring (8"" or more): 8d common (3 each direct joist)

**GYPSUM WALLBOARD:**
• ½"" thickness: 7"" o.c. nails, 12"" o.c. screws (16"" framing)
• ⅝"" thickness: 7"" o.c. nails, 12"" o.c. screws (16"" framing)
• Use No. 13 gauge nails or No. 6 screws meeting ASTM C514",
                    Confidence = 0.95f,
                    Sources = new[] { "780 CMR 120.Q", "Massachusetts Fastening Schedule", "10th Edition Building Code" }
                };
            }
            else if (query.Contains("plumbing") || query.Contains("pipe") || query.Contains("water") || query.Contains("sewer") || query.Contains("gas"))
            {
                return new {
                    Response = @"**MASSACHUSETTS PLUMBING CODES (248 CMR)**

**Key Requirements:**
• All plumbing work governed by 248 CMR: Board of State Examiners of Plumbers and Gas Fitters
• Licensed plumber required for gas and plumbing installations
• Permits required for new installations and major repairs

**Gas Appliances:**
• Gas fired appliances governed by 248 CMR
• Oil fired appliances governed by 527 CMR 1.00: Fire Safety Code
• Use only approved gas connectors and fittings
• Pressure testing required on all new gas lines

**Water/Sewer Connections:**
• Connection permits required from local authority
• Backflow prevention devices required per local codes
• Water service sizing based on fixture unit calculations
• Septic systems require Board of Health approval

**Common Applications:**
• Rough-in inspections before concealment
• Final inspections before occupancy
• Fixture installations and connections
• Gas line extensions and new meters",
                    Confidence = 0.90f,
                    Sources = new[] { "248 CMR", "Massachusetts Plumbing Code", "527 CMR Fire Safety" }
                };
            }
            else if (query.Contains("electrical") || query.Contains("wire") || query.Contains("outlet") || query.Contains("panel") || query.Contains("circuit"))
            {
                return new {
                    Response = @"**MASSACHUSETTS ELECTRICAL CODE (527 CMR 12.00)**

**Key Requirements:**
• All electrical work governed by 527 CMR 12.00: Massachusetts Electrical Code
• Licensed electrician required for most electrical work
• Permits required for new circuits, panels, and major modifications

**Common Residential Requirements:**
• GFCI protection required in bathrooms, kitchens, garages, basements
• AFCI protection required in bedrooms and living areas
• Minimum 20-amp circuits for kitchen countertop outlets
• Dedicated circuits for major appliances

**Service and Panels:**
• 200-amp service standard for new construction
• Panel locations must meet clearance requirements
• Proper grounding and bonding required
• Emergency disconnects required per local amendments

**Inspections Required:**
• Rough-in inspection before concealment
• Final inspection before energizing
• Service entrance inspections for new services
• Certificate of compliance required for occupancy",
                    Confidence = 0.90f,
                    Sources = new[] { "527 CMR 12.00", "Massachusetts Electrical Code", "NEC Amendments" }
                };
            }
            else if (query.Contains("permit") || query.Contains("inspection") || query.Contains("building official") || query.Contains("code enforcement"))
            {
                return new {
                    Response = @"**MASSACHUSETTS PERMIT & INSPECTION PROCESS**

**Building Permits Required For:**
• New construction and additions
• Structural alterations and renovations
• Mechanical, electrical, and plumbing systems
• Roofing and siding (check local requirements)

**Specialized Permits:**
• Electrical: 527 CMR 12.00 (Licensed electrician required)
• Plumbing/Gas: 248 CMR (Licensed plumber required)
• Fire Protection: 527 CMR 1.00
• Architectural Access: 521 CMR

**Inspection Schedule:**
• Foundation inspection before concrete pour
• Framing inspection before concealment
• Rough-in inspections (electrical, plumbing, mechanical)
• Insulation inspection before drywall
• Final inspection before occupancy

**Code Enforcement:**
• Building official enforces 780 CMR
• Fire official enforces 527 CMR 1.00
• Appeals process available through local boards
• Certificate of Occupancy required for new construction

**10th Edition Notes:**
• Effective October 11, 2024
• Concurrency period through June 30, 2025
• Applications may use 9th or 10th edition until June 2025",
                    Confidence = 0.95f,
                    Sources = new[] { "780 CMR", "Massachusetts Building Code 10th Edition", "M.G.L. c. 143" }
                };
            }
            else
            {
                return new {
                    Response = "I can help you with building codes, permits, timelines, budgets, and construction hardware! Try asking about electrical requirements, plumbing codes, tension ties, fasteners, permits, inspections, or project scheduling. I'm here to make your construction projects smoother and code-compliant!",
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