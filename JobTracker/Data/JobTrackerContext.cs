using JobTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data
{
    public class JobTrackerContext : DbContext
    {
        public JobTrackerContext(DbContextOptions<JobTrackerContext> options)
            : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; } = null!;
        public DbSet<JobSection> JobSections { get; set; } = null!;
        public DbSet<JobImage> JobImages { get; set; } = null!;
        public DbSet<InspectionReminder> InspectionReminders { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<WorkTask> WorkTasks { get; set; } = null!;
        public DbSet<MaterialRequest> MaterialRequests { get; set; } = null!;
        public DbSet<ChangeRequest> ChangeRequests { get; set; } = null!;
        public DbSet<UserJobAssignment> UserJobAssignments { get; set; } = null!;
        public DbSet<Inspection> Inspections { get; set; } = null!;
        public DbSet<TimeEntry> TimeEntries { get; set; } = null!;
        public DbSet<PendingClockIn> PendingClockIns { get; set; } = null!;
        public DbSet<MaterialStore> MaterialStores { get; set; } = null!;
        public DbSet<MaterialRun> MaterialRuns { get; set; } = null!;
        public DbSet<LocationTracker> LocationTrackers { get; set; } = null!;
        public DbSet<LocationPing> LocationPings { get; set; } = null!;
        public DbSet<LocationVerificationRequest> LocationVerificationRequests { get; set; } = null!;
        public DbSet<LunchBreak> LunchBreaks { get; set; } = null!;
        public DbSet<IssueReport> IssueReports { get; set; } = null!;
        public DbSet<JobTracker.Security.SecurityAuditLog> SecurityAuditLogs { get; set; } = null!;
        public DbSet<JobTracker.Controllers.WorkSchedule> WorkSchedules { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Job>()
                .HasMany(j => j.Sections)
                .WithOne(s => s.Job)
                .HasForeignKey(s => s.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add audit fields to save changes
            modelBuilder.Entity<Job>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            modelBuilder.Entity<JobSection>().Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            // Configure JobImage relationships
            modelBuilder.Entity<JobImage>()
                .HasOne(i => i.Job)
                .WithMany()
                .HasForeignKey(i => i.JobId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<JobImage>()
                .HasOne(i => i.JobSection)
                .WithMany(s => s.Images)
                .HasForeignKey(i => i.JobSectionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure InspectionReminder relationships
            modelBuilder.Entity<InspectionReminder>()
                .HasOne(r => r.JobSection)
                .WithMany()
                .HasForeignKey(r => r.JobSectionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure User relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.CreatedBy)
                .WithMany()
                .HasForeignKey(u => u.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Company-Job relationships
            modelBuilder.Entity<Job>()
                .HasOne(j => j.Company)
                .WithMany(c => c.Jobs)
                .HasForeignKey(j => j.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Add indexes for better performance
            modelBuilder.Entity<Job>().HasIndex(j => j.Status);
            modelBuilder.Entity<JobSection>().HasIndex(js => js.Status);
            modelBuilder.Entity<JobSection>().HasIndex(js => js.SectionType);
            modelBuilder.Entity<JobImage>().HasIndex(i => i.IsMainImage);
            modelBuilder.Entity<JobImage>().HasIndex(i => i.JobSectionId);
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.PhoneNumber).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Role);
            modelBuilder.Entity<Company>().HasIndex(c => c.AccountNumber).IsUnique();
            modelBuilder.Entity<Company>().HasIndex(c => c.ContactEmail).IsUnique();
            modelBuilder.Entity<Company>().HasIndex(c => c.IsActive);
            modelBuilder.Entity<Job>().HasIndex(j => j.CompanyId);
            modelBuilder.Entity<User>().HasIndex(u => u.CompanyId);

            // Configure LocationTracker relationships
            modelBuilder.Entity<LocationTracker>()
                .HasOne(lt => lt.TimeEntry)
                .WithOne()
                .HasForeignKey<LocationTracker>(lt => lt.TimeEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LocationTracker>()
                .HasOne(lt => lt.User)
                .WithMany()
                .HasForeignKey(lt => lt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LocationTracker>()
                .HasOne(lt => lt.Job)
                .WithMany()
                .HasForeignKey(lt => lt.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure LocationPing relationships
            modelBuilder.Entity<LocationPing>()
                .HasOne(lp => lp.LocationTracker)
                .WithMany(lt => lt.LocationPings)
                .HasForeignKey(lp => lp.LocationTrackerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure LocationVerificationRequest relationships
            modelBuilder.Entity<LocationVerificationRequest>()
                .HasOne(lvr => lvr.LocationTracker)
                .WithMany(lt => lt.VerificationRequests)
                .HasForeignKey(lvr => lvr.LocationTrackerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LocationVerificationRequest>()
                .HasOne(lvr => lvr.User)
                .WithMany()
                .HasForeignKey(lvr => lvr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for location tracking performance
            modelBuilder.Entity<LocationTracker>().HasIndex(lt => new { lt.UserId, lt.IsActive });
            modelBuilder.Entity<LocationTracker>().HasIndex(lt => lt.NextLocationCheckAt);
            modelBuilder.Entity<LocationPing>().HasIndex(lp => lp.PingTime);
            modelBuilder.Entity<LocationVerificationRequest>().HasIndex(lvr => new { lvr.UserId, lvr.Status });
        }

        public override int SaveChanges()
        {
            AddAuditInfo();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddAuditInfo();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void AddAuditInfo()
        {
            var entities = ChangeTracker.Entries().Where(x => 
                (x.Entity is Job || x.Entity is JobSection) && 
                (x.State == EntityState.Added || x.State == EntityState.Modified));

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Modified)
                {
                    ((dynamic)entity.Entity).UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}