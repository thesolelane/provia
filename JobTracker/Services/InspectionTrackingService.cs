using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Services
{
    public class InspectionTrackingService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<InspectionTrackingService> _logger;

        public InspectionTrackingService(JobTrackerContext context, ILogger<InspectionTrackingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CheckAndCreateInspectionReminders(int jobSectionId)
        {
            var section = await _context.JobSections.FindAsync(jobSectionId);
            if (section == null) return;

            var sectionName = GetSectionName(section.SectionType);
            
            // Check if section requires inspections and create reminders
            switch (section.SectionType)
            {
                case 3: // Framing
                    await CreateBuildingInspectionReminder(section, "Framing");
                    break;
                case 4: // Electrical Rough-in
                    await CreateElectricalInspectionReminder(section, "Electrical Rough-in");
                    break;
                case 5: // Plumbing Rough-in
                    await CreatePlumbingInspectionReminder(section, "Plumbing Rough-in");
                    break;
            }
        }

        public async Task<List<InspectionReminder>> GetPendingReminders(int? jobId = null)
        {
            var query = _context.InspectionReminders
                .Include(r => r.JobSection)
                .ThenInclude(s => s.Job)
                .Where(r => !r.IsCompleted);

            if (jobId.HasValue)
            {
                query = query.Where(r => r.JobSection.JobId == jobId.Value);
            }

            var reminders = await query.OrderByDescending(r => r.CreatedDate).ToListAsync();
            
            // Update priority based on age
            foreach (var reminder in reminders)
            {
                var daysSinceCreated = (DateTime.UtcNow - reminder.CreatedDate).Days;
                if (daysSinceCreated >= 3)
                {
                    reminder.Priority = "Urgent";
                    reminder.IsUrgent = true;
                }
                else if (daysSinceCreated >= 1)
                {
                    reminder.Priority = "High";
                }
            }

            await _context.SaveChangesAsync();
            return reminders;
        }

        public async Task CompleteInspection(int reminderId, bool passed, string? notes = null)
        {
            var reminder = await _context.InspectionReminders
                .Include(r => r.JobSection)
                .FirstOrDefaultAsync(r => r.Id == reminderId);

            if (reminder == null) return;

            reminder.IsCompleted = true;
            reminder.CompletedDate = DateTime.UtcNow;

            // Update the corresponding inspection fields in JobSection
            switch (reminder.InspectionType.ToLower())
            {
                case "building":
                    reminder.JobSection.BuildingInspectionCompleted = true;
                    reminder.JobSection.BuildingInspectionDate = DateTime.UtcNow;
                    reminder.JobSection.BuildingInspectionPassed = passed;
                    reminder.JobSection.BuildingInspectionNotes = notes;
                    break;
                case "electrical":
                    reminder.JobSection.ElectricalInspectionCompleted = true;
                    reminder.JobSection.ElectricalInspectionDate = DateTime.UtcNow;
                    reminder.JobSection.ElectricalInspectionPassed = passed;
                    reminder.JobSection.ElectricalInspectionNotes = notes;
                    break;
                case "plumbing":
                    reminder.JobSection.PlumbingInspectionCompleted = true;
                    reminder.JobSection.PlumbingInspectionDate = DateTime.UtcNow;
                    reminder.JobSection.PlumbingInspectionPassed = passed;
                    reminder.JobSection.PlumbingInspectionNotes = notes;
                    break;
            }

            await _context.SaveChangesAsync();
        }

        private async Task CreateBuildingInspectionReminder(JobSection section, string sectionName)
        {
            if (await ReminderExists(section.Id, "Building")) return;

            var reminder = new InspectionReminder
            {
                JobSectionId = section.Id,
                InspectionType = "Building",
                SectionName = sectionName,
                Message = $"Building inspection required for completed {sectionName} work. Contact building inspector to schedule.",
                DueDate = DateTime.UtcNow.AddDays(2),
                Priority = "High"
            };

            section.BuildingInspectionRequired = true;
            section.RequiresInspection = true;

            _context.InspectionReminders.Add(reminder);
            await _context.SaveChangesAsync();
        }

        private async Task CreateElectricalInspectionReminder(JobSection section, string sectionName)
        {
            if (await ReminderExists(section.Id, "Electrical")) return;

            var reminder = new InspectionReminder
            {
                JobSectionId = section.Id,
                InspectionType = "Electrical",
                SectionName = sectionName,
                Message = $"Electrical inspection required for completed {sectionName} work. Contact electrical inspector to schedule.",
                DueDate = DateTime.UtcNow.AddDays(2),
                Priority = "High"
            };

            section.ElectricalInspectionRequired = true;
            section.RequiresInspection = true;

            _context.InspectionReminders.Add(reminder);
            await _context.SaveChangesAsync();
        }

        private async Task CreatePlumbingInspectionReminder(JobSection section, string sectionName)
        {
            if (await ReminderExists(section.Id, "Plumbing")) return;

            var reminder = new InspectionReminder
            {
                JobSectionId = section.Id,
                InspectionType = "Plumbing",
                SectionName = sectionName,
                Message = $"Plumbing inspection required for completed {sectionName} work. Contact plumbing inspector to schedule.",
                DueDate = DateTime.UtcNow.AddDays(2),
                Priority = "High"
            };

            section.PlumbingInspectionRequired = true;
            section.RequiresInspection = true;

            _context.InspectionReminders.Add(reminder);
            await _context.SaveChangesAsync();
        }

        private async Task<bool> ReminderExists(int jobSectionId, string inspectionType)
        {
            return await _context.InspectionReminders
                .AnyAsync(r => r.JobSectionId == jobSectionId && 
                              r.InspectionType == inspectionType && 
                              !r.IsCompleted);
        }

        private string GetSectionName(int sectionType)
        {
            return sectionType switch
            {
                1 => "Permits",
                2 => "Demolition",
                3 => "Foundation",
                4 => "Framing",
                5 => "Electrical",
                6 => "Plumbing",
                7 => "HVAC",
                8 => "Insulation",
                9 => "Drywall",
                10 => "Flooring",
                11 => "Final Inspection",
                _ => "Unknown"
            };
        }
    }
}