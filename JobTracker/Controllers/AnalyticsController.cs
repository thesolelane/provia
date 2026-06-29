using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Services;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(JobTrackerContext context, ITenantContext tenantContext, ILogger<AnalyticsController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        // GET: api/analytics/overview
        // Key summary numbers for the top cards
        [HttpGet("overview")]
        public async Task<ActionResult<object>> GetOverview()
        {
            try
            {
                var cid = _tenantContext.GetCurrentCompanyId();
                var now = DateTime.UtcNow;
                var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var lastMonthStart = monthStart.AddMonths(-1);

                var jobs    = await _context.Jobs.Where(j => j.CompanyId == cid).ToListAsync();
                var leads   = await _context.Leads.Where(l => l.CompanyId == cid).ToListAsync();
                var invoices = await _context.Invoices.Where(i => i.CompanyId == cid).ToListAsync();
                var vendors = await _context.Vendors.Where(v => v.CompanyId == cid && v.IsActive).CountAsync();
                var photos  = await _context.FieldPhotos.Where(p => p.CompanyId == cid).CountAsync();

                var activeJobs     = jobs.Count(j => j.Status == "InProgress" || j.Status == "PermitsPending" || j.Status == "InspectionPending");
                var completedJobs  = jobs.Count(j => j.Status == "Completed");
                var revThisMonth   = invoices.Where(i => i.Status == "paid" && i.PaidAt >= monthStart).Sum(i => i.Total);
                var revLastMonth   = invoices.Where(i => i.Status == "paid" && i.PaidAt >= lastMonthStart && i.PaidAt < monthStart).Sum(i => i.Total);
                var outstanding    = invoices.Where(i => i.Status == "sent" || i.Status == "partial").Sum(i => i.BalanceDue);
                var activeLeads    = leads.Count(l => l.Stage != "signed" && l.Stage != "lost");
                var convertedLeads = leads.Count(l => l.Stage == "signed");

                return Ok(new
                {
                    TotalJobs         = jobs.Count,
                    ActiveJobs        = activeJobs,
                    CompletedJobs     = completedJobs,
                    RevenueThisMonth  = revThisMonth,
                    RevenueLastMonth  = revLastMonth,
                    RevenueMoMChange  = revLastMonth > 0 ? Math.Round((double)(revThisMonth - revLastMonth) / (double)revLastMonth * 100, 1) : 0,
                    Outstanding       = outstanding,
                    TotalLeads        = leads.Count,
                    ActiveLeads       = activeLeads,
                    ConvertedLeads    = convertedLeads,
                    ConversionRate    = leads.Count > 0 ? Math.Round((double)convertedLeads / leads.Count * 100, 1) : 0,
                    ActiveVendors     = vendors,
                    TotalPhotos       = photos,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching analytics overview");
                return StatusCode(500, new { message = "Error fetching overview" });
            }
        }

        // GET: api/analytics/jobs
        // Job pipeline by status + jobs created per month (12 months)
        [HttpGet("jobs")]
        public async Task<ActionResult<object>> GetJobAnalytics()
        {
            try
            {
                var cid = _tenantContext.GetCurrentCompanyId();
                var now = DateTime.UtcNow;
                var jobs = await _context.Jobs.Where(j => j.CompanyId == cid).ToListAsync();

                // Pipeline counts
                var pipeline = new[]
                {
                    new { Status = "Planning",           Label = "Planning",            Count = jobs.Count(j => j.Status == "Planning") },
                    new { Status = "PermitsPending",     Label = "Permits Pending",     Count = jobs.Count(j => j.Status == "PermitsPending") },
                    new { Status = "InProgress",         Label = "In Progress",         Count = jobs.Count(j => j.Status == "InProgress") },
                    new { Status = "InspectionPending",  Label = "Inspection Pending",  Count = jobs.Count(j => j.Status == "InspectionPending") },
                    new { Status = "OnHold",             Label = "On Hold",             Count = jobs.Count(j => j.Status == "OnHold") },
                    new { Status = "Completed",          Label = "Completed",           Count = jobs.Count(j => j.Status == "Completed") },
                    new { Status = "Cancelled",          Label = "Cancelled",           Count = jobs.Count(j => j.Status == "Cancelled") },
                };

                // Jobs created per month — last 12 months
                var months = Enumerable.Range(0, 12)
                    .Select(i => now.AddMonths(-11 + i))
                    .Select(d => new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc))
                    .ToList();

                var windowStart = months.First();
                var recentJobs  = jobs.Where(j => j.CreatedAt >= windowStart).ToList();

                var createdByMonth = months.Select(m => new
                {
                    label = m.ToString("MMM yy"),
                    count = recentJobs.Count(j => j.CreatedAt.Year == m.Year && j.CreatedAt.Month == m.Month),
                }).ToList();

                // Avg days to complete (for completed jobs with dates)
                var completedWithDates = jobs
                    .Where(j => j.Status == "Completed" && j.ActualCompletionDate.HasValue)
                    .Select(j => (j.ActualCompletionDate!.Value - j.CreatedAt).TotalDays)
                    .ToList();
                var avgDaysToComplete = completedWithDates.Any() ? Math.Round(completedWithDates.Average(), 1) : 0;

                return Ok(new { Pipeline = pipeline, CreatedByMonth = createdByMonth, AvgDaysToComplete = avgDaysToComplete });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching job analytics");
                return StatusCode(500, new { message = "Error fetching job analytics" });
            }
        }

        // GET: api/analytics/leads
        // Lead funnel by stage + leads created per month
        [HttpGet("leads")]
        public async Task<ActionResult<object>> GetLeadAnalytics()
        {
            try
            {
                var cid   = _tenantContext.GetCurrentCompanyId();
                var now   = DateTime.UtcNow;
                var leads = await _context.Leads.Where(l => l.CompanyId == cid).ToListAsync();

                var stages = new[]
                {
                    new { Stage = "incoming",           Label = "New Lead",            Count = leads.Count(l => l.Stage == "incoming") },
                    new { Stage = "callback_done",      Label = "Callback Done",       Count = leads.Count(l => l.Stage == "callback_done") },
                    new { Stage = "appointment_booked", Label = "Appointment Booked",  Count = leads.Count(l => l.Stage == "appointment_booked") },
                    new { Stage = "site_visit_done",    Label = "Site Visit Done",     Count = leads.Count(l => l.Stage == "site_visit_done") },
                    new { Stage = "quote_sent",         Label = "Quote Sent",          Count = leads.Count(l => l.Stage == "quote_sent") },
                    new { Stage = "follow_up",          Label = "Follow Up",           Count = leads.Count(l => l.Stage == "follow_up") },
                    new { Stage = "signed",             Label = "Signed / Won",        Count = leads.Count(l => l.Stage == "signed") },
                };

                // Leads by month — last 12
                var months = Enumerable.Range(0, 12)
                    .Select(i => now.AddMonths(-11 + i))
                    .Select(d => new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc))
                    .ToList();
                var windowStart   = months.First();
                var recentLeads   = leads.Where(l => l.CreatedAt >= windowStart).ToList();
                var byMonth = months.Select(m => new
                {
                    label  = m.ToString("MMM yy"),
                    total  = recentLeads.Count(l => l.CreatedAt.Year == m.Year && l.CreatedAt.Month == m.Month),
                    signed = recentLeads.Count(l => l.Stage == "signed" && l.CreatedAt.Year == m.Year && l.CreatedAt.Month == m.Month),
                }).ToList();

                return Ok(new { Stages = stages, ByMonth = byMonth, Total = leads.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching lead analytics");
                return StatusCode(500, new { message = "Error fetching lead analytics" });
            }
        }

        // GET: api/analytics/revenue
        // 12-month revenue trend + invoice status breakdown
        [HttpGet("revenue")]
        public async Task<ActionResult<object>> GetRevenueAnalytics()
        {
            try
            {
                var cid      = _tenantContext.GetCurrentCompanyId();
                var now      = DateTime.UtcNow;
                var invoices = await _context.Invoices.Where(i => i.CompanyId == cid).ToListAsync();

                var months = Enumerable.Range(0, 12)
                    .Select(i => now.AddMonths(-11 + i))
                    .Select(d => new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc))
                    .ToList();

                var windowStart = months.First();
                var paidInMonth = invoices.Where(i => i.Status == "paid" && i.PaidAt >= windowStart).ToList();
                var issuedInMonth = invoices.Where(i => i.IssuedAt >= windowStart).ToList();

                var trend = months.Select(m => new
                {
                    label    = m.ToString("MMM yy"),
                    collected = paidInMonth
                        .Where(i => i.PaidAt!.Value.Year == m.Year && i.PaidAt!.Value.Month == m.Month)
                        .Sum(i => i.Total),
                    invoiced = issuedInMonth
                        .Where(i => i.IssuedAt.HasValue && i.IssuedAt.Value.Year == m.Year && i.IssuedAt.Value.Month == m.Month)
                        .Sum(i => i.Total),
                }).ToList();

                var statusBreakdown = new[]
                {
                    new { Status = "draft",   Label = "Draft",    Count = invoices.Count(i => i.Status == "draft"),    Total = invoices.Where(i => i.Status == "draft").Sum(i => i.Total) },
                    new { Status = "sent",    Label = "Sent",     Count = invoices.Count(i => i.Status == "sent"),     Total = invoices.Where(i => i.Status == "sent").Sum(i => i.Total) },
                    new { Status = "partial", Label = "Partial",  Count = invoices.Count(i => i.Status == "partial"),  Total = invoices.Where(i => i.Status == "partial").Sum(i => i.Total) },
                    new { Status = "paid",    Label = "Paid",     Count = invoices.Count(i => i.Status == "paid"),     Total = invoices.Where(i => i.Status == "paid").Sum(i => i.Total) },
                    new { Status = "overdue", Label = "Overdue",  Count = invoices.Count(i => i.Status == "overdue"),  Total = invoices.Where(i => i.Status == "overdue").Sum(i => i.Total) },
                };

                return Ok(new
                {
                    Trend           = trend,
                    StatusBreakdown = statusBreakdown,
                    TotalInvoiced   = invoices.Sum(i => i.Total),
                    TotalCollected  = invoices.Where(i => i.Status == "paid").Sum(i => i.Total),
                    CollectionRate  = invoices.Sum(i => i.Total) > 0
                        ? Math.Round((double)invoices.Where(i => i.Status == "paid").Sum(i => i.Total) / (double)invoices.Sum(i => i.Total) * 100, 1)
                        : 0,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching revenue analytics");
                return StatusCode(500, new { message = "Error fetching revenue analytics" });
            }
        }

        // GET: api/analytics/vendors
        // Spend by category + top vendors by total spend
        [HttpGet("vendors")]
        public async Task<ActionResult<object>> GetVendorAnalytics()
        {
            try
            {
                var cid       = _tenantContext.GetCurrentCompanyId();
                var purchases = await _context.VendorPurchases
                    .Where(p => p.CompanyId == cid)
                    .Join(_context.Vendors, p => p.VendorId, v => v.Id, (p, v) => new { p, v })
                    .ToListAsync();

                var byCategory = purchases
                    .GroupBy(x => x.v.Category ?? "general")
                    .Select(g => new { Category = g.Key, Total = g.Sum(x => x.p.Amount), Count = g.Count() })
                    .OrderByDescending(g => g.Total)
                    .ToList();

                var topVendors = purchases
                    .GroupBy(x => new { x.v.Id, x.v.Name })
                    .Select(g => new { g.Key.Id, g.Key.Name, Total = g.Sum(x => x.p.Amount), Count = g.Count() })
                    .OrderByDescending(g => g.Total)
                    .Take(8)
                    .ToList();

                var totalSpend = purchases.Sum(x => x.p.Amount);

                return Ok(new { ByCategory = byCategory, TopVendors = topVendors, TotalSpend = totalSpend });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching vendor analytics");
                return StatusCode(500, new { message = "Error fetching vendor analytics" });
            }
        }
    }
}
