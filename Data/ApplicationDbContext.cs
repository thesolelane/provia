using JobTrackerApp.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTrackerApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobSection> JobSections { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<JobAssignment> JobAssignments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Subcontractor> Subcontractors { get; set; }
        public DbSet<SectionSubcontractor> SectionSubcontractors { get; set; }
        public DbSet<BuildingCode> BuildingCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Job entity configurations
            modelBuilder.Entity<Job>()
                .HasMany(j => j.Sections)
                .WithOne(s => s.Job)
                .HasForeignKey(s => s.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Job>()
                .HasMany(j => j.Assignments)
                .WithOne(a => a.Job)
                .HasForeignKey(a => a.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobSection entity configurations
            modelBuilder.Entity<JobSection>()
                .HasOne(js => js.ResponsibleEmployee)
                .WithMany(e => e.ResponsibleForSections)
                .HasForeignKey(js => js.ResponsibleEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<JobSection>()
                .HasOne(js => js.Subcontractor)
                .WithMany(s => s.Sections)
                .HasForeignKey(js => js.SubcontractorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Employee entity configurations
            modelBuilder.Entity<Employee>()
                .HasMany(e => e.TimeEntries)
                .WithOne(te => te.Employee)
                .HasForeignKey(te => te.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Employee>()
                .HasMany(e => e.JobAssignments)
                .WithOne(ja => ja.Employee)
                .HasForeignKey(ja => ja.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Section Subcontractor relationship
            modelBuilder.Entity<SectionSubcontractor>()
                .HasOne(ss => ss.Section)
                .WithMany(js => js.SectionSubcontractors)
                .HasForeignKey(ss => ss.SectionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SectionSubcontractor>()
                .HasOne(ss => ss.Subcontractor)
                .WithMany(s => s.SectionSubcontractors)
                .HasForeignKey(ss => ss.SubcontractorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for faster queries
            modelBuilder.Entity<Job>().HasIndex(j => j.JobNumber);
            modelBuilder.Entity<Employee>().HasIndex(e => e.EmployeeNumber);
            modelBuilder.Entity<Employee>().HasIndex(e => e.ActiveDirectoryId);
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.ActiveDirectoryId);
            modelBuilder.Entity<BuildingCode>().HasIndex(bc => bc.CodeNumber);
            modelBuilder.Entity<TimeEntry>().HasIndex(te => te.ClockInTime);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is Job || 
                           e.Entity is JobSection || 
                           e.Entity is Employee || 
                           e.Entity is TimeEntry || 
                           e.Entity is JobAssignment || 
                           e.Entity is User || 
                           e.Entity is Subcontractor || 
                           e.Entity is SectionSubcontractor || 
                           e.Entity is BuildingCode);

            foreach (var entityEntry in entries)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    // Set CreatedAt property to current date for new entities
                    entityEntry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                }

                if (entityEntry.State == EntityState.Modified)
                {
                    // Don't modify the CreatedAt value, set the UpdatedAt value
                    entityEntry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                    // Keep the original CreatedAt value
                    entityEntry.Property("CreatedAt").IsModified = false;
                }
            }
        }
    }
}
