using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.AspNetCore.Authorization;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConstructionCodesController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<ConstructionCodesController> _logger;

        public ConstructionCodesController(JobTrackerContext context, ILogger<ConstructionCodesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("cost-types")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CostTypeDto>>> GetCostTypes()
        {
            var costTypes = await _context.CostTypes
                .Where(ct => ct.IsActive)
                .OrderBy(ct => ct.Code)
                .Select(ct => new CostTypeDto
                {
                    Code = ct.Code,
                    Label = ct.Label,
                    Description = ct.Description
                })
                .ToListAsync();

            return Ok(costTypes);
        }

        [HttpGet("departments")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
        {
            var departments = await _context.ConstructionDepartments
                .Where(d => d.IsActive)
                .Include(d => d.Subcategories!.Where(s => s.IsActive))
                .OrderBy(d => d.DeptCode)
                .Select(d => new DepartmentDto
                {
                    DeptCode = d.DeptCode,
                    Name = d.Name,
                    Description = d.Description,
                    Subcategories = d.Subcategories!
                        .OrderBy(s => s.SubCode)
                        .Select(s => new SubcategoryDto
                        {
                            DeptCode = s.DeptCode,
                            SubCode = s.SubCode,
                            Label = s.Label,
                            Description = s.Description
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(departments);
        }

        [HttpGet("codes")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ConstructionCodeDto>>> GetCodes(
            [FromQuery] int? deptCode = null,
            [FromQuery] int? costType = null)
        {
            var query = _context.ConstructionCodes.Where(c => c.IsActive);

            if (deptCode.HasValue)
                query = query.Where(c => c.DeptCode == deptCode.Value);

            if (costType.HasValue)
                query = query.Where(c => c.CostTypeCode == costType.Value);

            var codes = await query
                .OrderBy(c => c.FullCode)
                .Select(c => new ConstructionCodeDto
                {
                    FullCode = c.FullCode,
                    DeptCode = c.DeptCode,
                    CostTypeCode = c.CostTypeCode,
                    SubCode = c.SubCode,
                    DeptName = c.DeptName,
                    CostTypeLabel = c.CostTypeLabel,
                    SubLabel = c.SubLabel,
                    DisplayName = c.DisplayName,
                    WaveProductId = c.WaveProductId,
                    LastSyncedToWave = c.LastSyncedToWave
                })
                .ToListAsync();

            return Ok(codes);
        }

        [HttpGet("codes/{fullCode}")]
        [AllowAnonymous]
        public async Task<ActionResult<ConstructionCodeDto>> GetCode(string fullCode)
        {
            var code = await _context.ConstructionCodes
                .Where(c => c.FullCode == fullCode)
                .Select(c => new ConstructionCodeDto
                {
                    FullCode = c.FullCode,
                    DeptCode = c.DeptCode,
                    CostTypeCode = c.CostTypeCode,
                    SubCode = c.SubCode,
                    DeptName = c.DeptName,
                    CostTypeLabel = c.CostTypeLabel,
                    SubLabel = c.SubLabel,
                    DisplayName = c.DisplayName,
                    WaveProductId = c.WaveProductId,
                    LastSyncedToWave = c.LastSyncedToWave
                })
                .FirstOrDefaultAsync();

            if (code == null)
                return NotFound(new { message = $"Code {fullCode} not found" });

            return Ok(code);
        }

        [HttpPost("seed")]
        [AllowAnonymous]
        public async Task<ActionResult> SeedConstructionCodes()
        {
            try
            {
                var existingCostTypes = await _context.CostTypes.CountAsync();
                if (existingCostTypes > 0)
                {
                    return Ok(new { message = "Construction codes already seeded", 
                        costTypes = await _context.CostTypes.CountAsync(),
                        departments = await _context.ConstructionDepartments.CountAsync(),
                        subcategories = await _context.DepartmentSubcategories.CountAsync(),
                        codes = await _context.ConstructionCodes.CountAsync()
                    });
                }

                // Seed Cost Types
                var costTypes = new List<CostType>
                {
                    new() { Code = 0, Label = "Misc / Uncategorized", Description = "Miscellaneous or uncategorized costs" },
                    new() { Code = 1, Label = "Labor", Description = "Labor costs for work performed" },
                    new() { Code = 2, Label = "Material", Description = "Material and supplies costs" },
                    new() { Code = 3, Label = "Equipment / Rentals", Description = "Equipment rental and tool costs" },
                    new() { Code = 4, Label = "Subcontract", Description = "Subcontractor lump sum costs" },
                    new() { Code = 5, Label = "Fees / Permits / Disposal", Description = "Fees, permits, and disposal costs" }
                };
                await _context.CostTypes.AddRangeAsync(costTypes);

                // Seed Departments
                var departments = new List<ConstructionDepartment>
                {
                    new() { DeptCode = 100, Name = "General Conditions", DisplayOrder = 1 },
                    new() { DeptCode = 200, Name = "Demolition", DisplayOrder = 2 },
                    new() { DeptCode = 300, Name = "Site/Excavation/Concrete", DisplayOrder = 3 },
                    new() { DeptCode = 400, Name = "Framing/Rough Carpentry", DisplayOrder = 4 },
                    new() { DeptCode = 410, Name = "Roofing", DisplayOrder = 5 },
                    new() { DeptCode = 420, Name = "Siding & Exterior Trim", DisplayOrder = 6 },
                    new() { DeptCode = 430, Name = "Windows & Exterior Doors", DisplayOrder = 7 },
                    new() { DeptCode = 500, Name = "Masonry/Foundations/Chimneys", DisplayOrder = 8 },
                    new() { DeptCode = 600, Name = "Plumbing", DisplayOrder = 9 },
                    new() { DeptCode = 610, Name = "HVAC", DisplayOrder = 10 },
                    new() { DeptCode = 620, Name = "Sheet Metal", DisplayOrder = 11 },
                    new() { DeptCode = 630, Name = "Electrical", DisplayOrder = 12 },
                    new() { DeptCode = 640, Name = "Fire/Life Safety", DisplayOrder = 13 },
                    new() { DeptCode = 700, Name = "Insulation", DisplayOrder = 14 },
                    new() { DeptCode = 710, Name = "Drywall/Plaster", DisplayOrder = 15 },
                    new() { DeptCode = 720, Name = "Flooring", DisplayOrder = 16 },
                    new() { DeptCode = 730, Name = "Tile", DisplayOrder = 17 },
                    new() { DeptCode = 740, Name = "Painting", DisplayOrder = 18 },
                    new() { DeptCode = 750, Name = "Finish Carpentry/Millwork", DisplayOrder = 19 },
                    new() { DeptCode = 800, Name = "Specialties", DisplayOrder = 20 },
                    new() { DeptCode = 900, Name = "Overhead/Contingency", DisplayOrder = 21 }
                };
                await _context.ConstructionDepartments.AddRangeAsync(departments);
                await _context.SaveChangesAsync();

                // Seed Subcategories per Department
                var subcategories = new List<DepartmentSubcategory>();

                // 100 - General Conditions
                subcategories.AddRange(CreateSubcategories(100, new[]
                {
                    ("01", "Project management"),
                    ("02", "Site supervision"),
                    ("03", "Temporary utilities"),
                    ("04", "Site security"),
                    ("05", "Cleanup/debris removal"),
                    ("00", "Misc general conditions")
                }));

                // 200 - Demolition
                subcategories.AddRange(CreateSubcategories(200, new[]
                {
                    ("01", "Interior demolition"),
                    ("02", "Exterior demolition"),
                    ("03", "Structural demolition"),
                    ("04", "Hazmat abatement"),
                    ("05", "Debris hauling"),
                    ("00", "Misc demolition")
                }));

                // 300 - Site/Excavation/Concrete
                subcategories.AddRange(CreateSubcategories(300, new[]
                {
                    ("01", "Excavation"),
                    ("02", "Grading"),
                    ("03", "Foundation concrete"),
                    ("04", "Flatwork/slabs"),
                    ("05", "Retaining walls"),
                    ("06", "Drainage"),
                    ("00", "Misc site/concrete")
                }));

                // 400 - Framing/Rough Carpentry
                subcategories.AddRange(CreateSubcategories(400, new[]
                {
                    ("01", "Wall framing"),
                    ("02", "Floor framing"),
                    ("03", "Roof framing"),
                    ("04", "Deck framing"),
                    ("05", "Sheathing"),
                    ("06", "Blocking/backing"),
                    ("00", "Misc framing")
                }));

                // 410 - Roofing
                subcategories.AddRange(CreateSubcategories(410, new[]
                {
                    ("01", "Shingle roofing"),
                    ("02", "Metal roofing"),
                    ("03", "Flat/membrane roofing"),
                    ("04", "Flashing/trim"),
                    ("05", "Gutters/downspouts"),
                    ("00", "Misc roofing")
                }));

                // 420 - Siding & Exterior Trim
                subcategories.AddRange(CreateSubcategories(420, new[]
                {
                    ("01", "Vinyl siding"),
                    ("02", "Wood siding"),
                    ("03", "Fiber cement siding"),
                    ("04", "Exterior trim"),
                    ("05", "Soffit/fascia"),
                    ("00", "Misc siding")
                }));

                // 430 - Windows & Exterior Doors
                subcategories.AddRange(CreateSubcategories(430, new[]
                {
                    ("01", "Windows"),
                    ("02", "Entry doors"),
                    ("03", "Patio/sliding doors"),
                    ("04", "Garage doors"),
                    ("05", "Skylights"),
                    ("00", "Misc windows/doors")
                }));

                // 500 - Masonry/Foundations/Chimneys
                subcategories.AddRange(CreateSubcategories(500, new[]
                {
                    ("01", "Block/brick masonry"),
                    ("02", "Stone veneer"),
                    ("03", "Chimney work"),
                    ("04", "Foundation repair"),
                    ("05", "Waterproofing"),
                    ("00", "Misc masonry")
                }));

                // 600 - Plumbing
                subcategories.AddRange(CreateSubcategories(600, new[]
                {
                    ("01", "Water supply"),
                    ("02", "Sewer/DWV"),
                    ("03", "Heating/hydronic"),
                    ("04", "Gas piping"),
                    ("05", "Fixtures"),
                    ("06", "Water heater/boiler"),
                    ("07", "Backflow/PRV"),
                    ("00", "Misc plumbing")
                }));

                // 610 - HVAC
                subcategories.AddRange(CreateSubcategories(610, new[]
                {
                    ("01", "Ductwork"),
                    ("02", "Furnace/air handler"),
                    ("03", "Condensing unit"),
                    ("04", "Mini-split systems"),
                    ("05", "Ventilation"),
                    ("06", "Controls/thermostats"),
                    ("00", "Misc HVAC")
                }));

                // 620 - Sheet Metal
                subcategories.AddRange(CreateSubcategories(620, new[]
                {
                    ("01", "HVAC ductwork fabrication"),
                    ("02", "Flashing/trim"),
                    ("03", "Custom metal work"),
                    ("00", "Misc sheet metal")
                }));

                // 630 - Electrical
                subcategories.AddRange(CreateSubcategories(630, new[]
                {
                    ("01", "Rough wiring"),
                    ("02", "Finish devices"),
                    ("03", "Service/panel/meter"),
                    ("04", "Low-voltage/data"),
                    ("05", "Smoke/CO systems"),
                    ("06", "Lighting fixtures"),
                    ("00", "Misc electrical")
                }));

                // 640 - Fire/Life Safety
                subcategories.AddRange(CreateSubcategories(640, new[]
                {
                    ("01", "Fire sprinkler system"),
                    ("02", "Fire alarm system"),
                    ("03", "Emergency lighting"),
                    ("04", "Fire extinguishers"),
                    ("00", "Misc fire safety")
                }));

                // 700 - Insulation
                subcategories.AddRange(CreateSubcategories(700, new[]
                {
                    ("01", "Batt insulation"),
                    ("02", "Blown insulation"),
                    ("03", "Spray foam"),
                    ("04", "Rigid board"),
                    ("05", "Vapor barrier"),
                    ("00", "Misc insulation")
                }));

                // 710 - Drywall/Plaster
                subcategories.AddRange(CreateSubcategories(710, new[]
                {
                    ("01", "Drywall hanging"),
                    ("02", "Drywall finishing"),
                    ("03", "Plaster repair"),
                    ("04", "Texture application"),
                    ("00", "Misc drywall")
                }));

                // 720 - Flooring
                subcategories.AddRange(CreateSubcategories(720, new[]
                {
                    ("01", "Hardwood"),
                    ("02", "Tile flooring"),
                    ("03", "Carpet"),
                    ("04", "Vinyl/LVP"),
                    ("05", "Underlayment"),
                    ("06", "Subfloor leveling"),
                    ("00", "Misc flooring")
                }));

                // 730 - Tile
                subcategories.AddRange(CreateSubcategories(730, new[]
                {
                    ("01", "Wall tile"),
                    ("02", "Floor tile"),
                    ("03", "Shower/tub tile"),
                    ("04", "Backsplash"),
                    ("05", "Stone/marble"),
                    ("00", "Misc tile")
                }));

                // 740 - Painting
                subcategories.AddRange(CreateSubcategories(740, new[]
                {
                    ("01", "Interior painting"),
                    ("02", "Exterior painting"),
                    ("03", "Staining/finishing"),
                    ("04", "Wallcovering"),
                    ("05", "Prep work"),
                    ("00", "Misc painting")
                }));

                // 750 - Finish Carpentry/Millwork
                subcategories.AddRange(CreateSubcategories(750, new[]
                {
                    ("01", "Trim/molding"),
                    ("02", "Interior doors"),
                    ("03", "Cabinetry"),
                    ("04", "Countertops"),
                    ("05", "Stairs/railings"),
                    ("06", "Built-ins/shelving"),
                    ("00", "Misc finish carpentry")
                }));

                // 800 - Specialties
                subcategories.AddRange(CreateSubcategories(800, new[]
                {
                    ("01", "Appliances"),
                    ("02", "Hardware/accessories"),
                    ("03", "Mirrors/glass"),
                    ("04", "Signage"),
                    ("00", "Misc specialties")
                }));

                // 900 - Overhead/Contingency
                subcategories.AddRange(CreateSubcategories(900, new[]
                {
                    ("01", "Company overhead"),
                    ("02", "Contingency"),
                    ("03", "Profit margin"),
                    ("04", "Insurance"),
                    ("05", "Bonding"),
                    ("00", "Misc overhead")
                }));

                await _context.DepartmentSubcategories.AddRangeAsync(subcategories);
                await _context.SaveChangesAsync();

                // Generate all 6-digit codes
                await GenerateAllCodes();

                return Ok(new
                {
                    message = "Construction codes seeded successfully",
                    costTypes = await _context.CostTypes.CountAsync(),
                    departments = await _context.ConstructionDepartments.CountAsync(),
                    subcategories = await _context.DepartmentSubcategories.CountAsync(),
                    codes = await _context.ConstructionCodes.CountAsync()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding construction codes");
                return StatusCode(500, new { message = "Error seeding codes", error = ex.Message });
            }
        }

        private List<DepartmentSubcategory> CreateSubcategories(int deptCode, (string code, string label)[] subs)
        {
            var dept = _context.ConstructionDepartments.Local.FirstOrDefault(d => d.DeptCode == deptCode);
            return subs.Select((s, i) => new DepartmentSubcategory
            {
                DeptCode = deptCode,
                Department = dept,
                SubCode = s.code,
                Label = s.label,
                DisplayOrder = i
            }).ToList();
        }

        private async Task GenerateAllCodes()
        {
            var departments = await _context.ConstructionDepartments.ToListAsync();
            var costTypes = await _context.CostTypes.ToListAsync();
            var subcategories = await _context.DepartmentSubcategories.ToListAsync();

            var codes = new List<ConstructionCode>();

            foreach (var dept in departments)
            {
                var deptSubs = subcategories.Where(s => s.DeptCode == dept.DeptCode).ToList();

                foreach (var costType in costTypes)
                {
                    foreach (var sub in deptSubs)
                    {
                        var fullCode = $"{dept.DeptCode:D3}{costType.Code}{sub.SubCode}";
                        var displayName = $"{dept.Name} - {costType.Label} - {sub.Label}";

                        codes.Add(new ConstructionCode
                        {
                            FullCode = fullCode,
                            DeptCode = dept.DeptCode,
                            CostTypeCode = costType.Code,
                            SubCode = sub.SubCode,
                            DeptName = dept.Name,
                            CostTypeLabel = costType.Label,
                            SubLabel = sub.Label,
                            DisplayName = displayName,
                            DepartmentId = dept.Id,
                            SubcategoryId = sub.Id
                        });
                    }
                }
            }

            await _context.ConstructionCodes.AddRangeAsync(codes);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Generated {codes.Count} construction codes");
        }

        [HttpPost("generate")]
        [AllowAnonymous]
        public async Task<ActionResult> GenerateCodes([FromBody] GenerateCodesRequest? request)
        {
            try
            {
                if (request?.OverwriteExisting == true)
                {
                    _context.ConstructionCodes.RemoveRange(_context.ConstructionCodes);
                    await _context.SaveChangesAsync();
                }

                var existingCodes = await _context.ConstructionCodes.CountAsync();
                if (existingCodes > 0 && request?.OverwriteExisting != true)
                {
                    return BadRequest(new { message = "Codes already exist. Use overwriteExisting: true to regenerate." });
                }

                await GenerateAllCodes();

                return Ok(new
                {
                    message = "Codes generated successfully",
                    totalCodes = await _context.ConstructionCodes.CountAsync()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating codes");
                return StatusCode(500, new { message = "Error generating codes", error = ex.Message });
            }
        }

        [HttpGet("stats")]
        [AllowAnonymous]
        public async Task<ActionResult> GetStats()
        {
            return Ok(new
            {
                costTypes = await _context.CostTypes.CountAsync(),
                departments = await _context.ConstructionDepartments.CountAsync(),
                subcategories = await _context.DepartmentSubcategories.CountAsync(),
                totalCodes = await _context.ConstructionCodes.CountAsync(),
                syncedToWave = await _context.ConstructionCodes.CountAsync(c => c.WaveProductId != null)
            });
        }

        [HttpGet("lookup/{fullCode}")]
        [AllowAnonymous]
        public async Task<ActionResult> LookupCode(string fullCode)
        {
            if (fullCode.Length != 6)
                return BadRequest(new { message = "Code must be 6 digits" });

            var code = await _context.ConstructionCodes
                .Where(c => c.FullCode == fullCode)
                .FirstOrDefaultAsync();

            if (code == null)
            {
                // Parse and provide info even if not in DB
                var deptCode = int.Parse(fullCode.Substring(0, 3));
                var costTypeCode = int.Parse(fullCode.Substring(3, 1));
                var subCode = fullCode.Substring(4, 2);

                var dept = await _context.ConstructionDepartments
                    .FirstOrDefaultAsync(d => d.DeptCode == deptCode);
                var costType = await _context.CostTypes
                    .FirstOrDefaultAsync(ct => ct.Code == costTypeCode);
                var sub = await _context.DepartmentSubcategories
                    .FirstOrDefaultAsync(s => s.DeptCode == deptCode && s.SubCode == subCode);

                return Ok(new
                {
                    found = false,
                    parsed = new
                    {
                        fullCode,
                        deptCode,
                        deptName = dept?.Name,
                        costTypeCode,
                        costTypeLabel = costType?.Label,
                        subCode,
                        subLabel = sub?.Label
                    }
                });
            }

            return Ok(new
            {
                found = true,
                code = new ConstructionCodeDto
                {
                    FullCode = code.FullCode,
                    DeptCode = code.DeptCode,
                    CostTypeCode = code.CostTypeCode,
                    SubCode = code.SubCode,
                    DeptName = code.DeptName,
                    CostTypeLabel = code.CostTypeLabel,
                    SubLabel = code.SubLabel,
                    DisplayName = code.DisplayName,
                    WaveProductId = code.WaveProductId,
                    LastSyncedToWave = code.LastSyncedToWave
                }
            });
        }
    }
}
