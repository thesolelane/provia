using JobTrackerApp.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace JobTrackerApp.Data
{
    public class JobTrackerContext : DbContext
    {
        public JobTrackerContext(DbContextOptions<JobTrackerContext> options)
            : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobSection> JobSections { get; set; }
        public DbSet<Subcontractor> Subcontractors { get; set; }
        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<BuildingCode> BuildingCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Job>()
                .HasMany(j => j.Sections)
                .WithOne(s => s.Job)
                .HasForeignKey(s => s.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Job>()
                .HasMany(j => j.TimeEntries)
                .WithOne(t => t.Job)
                .HasForeignKey(t => t.JobId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasMany(u => u.TimeEntries)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Subcontractor>()
                .HasMany(s => s.JobSections)
                .WithOne(js => js.Subcontractor)
                .HasForeignKey(js => js.SubcontractorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Add audit fields to save changes
            modelBuilder.Entity<Job>().Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<JobSection>().Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<Subcontractor>().Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<TimeEntry>().Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<User>().Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<BuildingCode>().Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Add indexes for better performance
            modelBuilder.Entity<Job>().HasIndex(j => j.Status);
            modelBuilder.Entity<JobSection>().HasIndex(js => js.Status);
            modelBuilder.Entity<JobSection>().HasIndex(js => js.Type);
            modelBuilder.Entity<TimeEntry>().HasIndex(te => te.ClockInTime);
            modelBuilder.Entity<BuildingCode>().HasIndex(bc => bc.RelatedSection);
            modelBuilder.Entity<BuildingCode>().HasIndex(bc => bc.CodeNumber);
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
                (x.Entity is Job || 
                 x.Entity is JobSection || 
                 x.Entity is Subcontractor || 
                 x.Entity is TimeEntry || 
                 x.Entity is User ||
                 x.Entity is BuildingCode) && 
                (x.State == EntityState.Added || x.State == EntityState.Modified));

            // Get the current user from the HttpContext
            var currentUser = "System"; // This should be replaced with the actual username from HttpContext

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added)
                {
                    ((dynamic)entity.Entity).CreatedDate = DateTime.UtcNow;
                    ((dynamic)entity.Entity).CreatedBy = currentUser;
                }

                if (entity.State == EntityState.Modified)
                {
                    ((dynamic)entity.Entity).ModifiedDate = DateTime.UtcNow;
                    ((dynamic)entity.Entity).ModifiedBy = currentUser;
                }
            }
        }
    }
}
