using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;

namespace JobTracker.Services
{
    public class OverdueInvoiceService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<OverdueInvoiceService> _logger;
        private static readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public OverdueInvoiceService(IServiceProvider services, ILogger<OverdueInvoiceService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Overdue invoice service started.");

            // Run immediately on startup, then every hour
            while (!stoppingToken.IsCancellationRequested)
            {
                await MarkOverdueInvoicesAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task MarkOverdueInvoicesAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<JobTrackerContext>();

                var now = DateTime.UtcNow;

                // Find sent or partial invoices whose due date has passed
                var overdue = await context.Invoices
                    .Where(i =>
                        (i.Status == InvoiceStatuses.Sent || i.Status == InvoiceStatuses.Partial) &&
                        i.DueAt.HasValue &&
                        i.DueAt.Value < now)
                    .ToListAsync(ct);

                if (overdue.Count == 0)
                    return;

                foreach (var invoice in overdue)
                {
                    invoice.Status = InvoiceStatuses.Overdue;
                    invoice.UpdatedAt = now;
                }

                await context.SaveChangesAsync(ct);

                _logger.LogInformation("Marked {Count} invoice(s) as overdue.", overdue.Count);
            }
            catch (OperationCanceledException)
            {
                // Shutdown — ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in overdue invoice check.");
            }
        }
    }
}
