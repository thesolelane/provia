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

            // Add indexes for better performance
            modelBuilder.Entity<Job>().HasIndex(j => j.Status);
            modelBuilder.Entity<JobSection>().HasIndex(js => js.Status);
            modelBuilder.Entity<JobSection>().HasIndex(js => js.SectionType);
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