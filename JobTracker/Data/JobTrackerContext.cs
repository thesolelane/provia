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
        public DbSet<DeactivatedUser> DeactivatedUsers { get; set; } = null!;
        public DbSet<JobTracker.Security.SecurityAuditLog> SecurityAuditLogs { get; set; } = null!;
        public DbSet<JobTracker.Controllers.WorkSchedule> WorkSchedules { get; set; } = null!;
        public DbSet<SyncQueueItem> SyncQueueItems { get; set; } = null!;
        public DbSet<SyncDevice> SyncDevices { get; set; } = null!;
        public DbSet<SyncConflict> SyncConflicts { get; set; } = null!;
        public DbSet<SubContractorCompany> SubContractorCompanies { get; set; } = null!;
        public DbSet<ClientInfo> ClientInfos { get; set; } = null!;
        public DbSet<JobBid> JobBids { get; set; } = null!;
        public DbSet<InspectionStage> InspectionStages { get; set; } = null!;
        public DbSet<PermitDocument> PermitDocuments { get; set; } = null!;
        public DbSet<PermitFormTemplate> PermitFormTemplates { get; set; } = null!;
        public DbSet<PropertyProfile> PropertyProfiles { get; set; } = null!;
        public DbSet<FormFieldMapping> FormFieldMappings { get; set; } = null!;
        
        // Code Engine tables
        public DbSet<CodeBook> CodeBooks { get; set; } = null!;
        public DbSet<CodeRule> CodeRules { get; set; } = null!;
        public DbSet<ScopeCategory> ScopeCategories { get; set; } = null!;
        public DbSet<ScopeItem> ScopeItems { get; set; } = null!;
        public DbSet<JobScope> JobScopes { get; set; } = null!;
        public DbSet<TradeAssignment> TradeAssignments { get; set; } = null!;
        public DbSet<JobPermit> JobPermits { get; set; } = null!;

        // Construction Cost Code System (6-digit codes)
        public DbSet<CostType> CostTypes { get; set; } = null!;
        public DbSet<ConstructionDepartment> ConstructionDepartments { get; set; } = null!;
        public DbSet<DepartmentSubcategory> DepartmentSubcategories { get; set; } = null!;
        public DbSet<ConstructionCode> ConstructionCodes { get; set; } = null!;
        public DbSet<CodeSyncLog> CodeSyncLogs { get; set; } = null!;

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

            modelBuilder.Entity<InspectionReminder>()
                .HasOne(r => r.Company)
                .WithMany()
                .HasForeignKey(r => r.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

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
                
            // Configure TimeEntry relationships with Company
            modelBuilder.Entity<TimeEntry>()
                .HasOne(te => te.Company)
                .WithMany()
                .HasForeignKey(te => te.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure MaterialRun relationships with Company
            modelBuilder.Entity<MaterialRun>()
                .HasOne(mr => mr.Company)
                .WithMany()
                .HasForeignKey(mr => mr.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure LunchBreak relationships with Company
            modelBuilder.Entity<LunchBreak>()
                .HasOne(lb => lb.Company)
                .WithMany()
                .HasForeignKey(lb => lb.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure IssueReport relationships with Company
            modelBuilder.Entity<IssueReport>()
                .HasOne(ir => ir.Company)
                .WithMany()
                .HasForeignKey(ir => ir.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure SubContractorCompany relationships (multi-company support for subs)
            modelBuilder.Entity<SubContractorCompany>()
                .HasOne(sc => sc.SubContractorUser)
                .WithMany()
                .HasForeignKey(sc => sc.SubContractorUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubContractorCompany>()
                .HasOne(sc => sc.Company)
                .WithMany()
                .HasForeignKey(sc => sc.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubContractorCompany>()
                .HasIndex(sc => new { sc.SubContractorUserId, sc.CompanyId })
                .IsUnique(); // Prevent duplicate sub-contractor entries per company

            modelBuilder.Entity<SubContractorCompany>()
                .HasIndex(sc => sc.Status);

            modelBuilder.Entity<SubContractorCompany>()
                .HasIndex(sc => sc.CompanyId);

            // Configure MaterialStore relationships with Company
            modelBuilder.Entity<MaterialStore>()
                .HasOne(ms => ms.Company)
                .WithMany()
                .HasForeignKey(ms => ms.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure JobSection relationships with Company
            modelBuilder.Entity<JobSection>()
                .HasOne(js => js.Company)
                .WithMany()
                .HasForeignKey(js => js.CompanyId)
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
            modelBuilder.Entity<TimeEntry>().HasIndex(te => te.CompanyId);
            modelBuilder.Entity<MaterialRun>().HasIndex(mr => mr.CompanyId);
            modelBuilder.Entity<LunchBreak>().HasIndex(lb => lb.CompanyId);
            modelBuilder.Entity<IssueReport>().HasIndex(ir => ir.CompanyId);
            modelBuilder.Entity<MaterialStore>().HasIndex(ms => ms.CompanyId);
            modelBuilder.Entity<JobSection>().HasIndex(js => js.CompanyId);
            modelBuilder.Entity<InspectionReminder>().HasIndex(ir => ir.CompanyId);

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

            // Configure DeactivatedUser relationships
            modelBuilder.Entity<DeactivatedUser>()
                .HasOne(du => du.DeactivatedBy)
                .WithMany()
                .HasForeignKey(du => du.DeactivatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<DeactivatedUser>()
                .HasOne(du => du.Company)
                .WithMany()
                .HasForeignKey(du => du.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add indexes for deactivated users
            modelBuilder.Entity<DeactivatedUser>().HasIndex(du => du.OriginalUserId);
            modelBuilder.Entity<DeactivatedUser>().HasIndex(du => du.DeactivatedAt);
            modelBuilder.Entity<DeactivatedUser>().HasIndex(du => du.CanBeReactivated);
            modelBuilder.Entity<DeactivatedUser>().HasIndex(du => du.CompanyId);

            // Configure Construction Code System relationships
            modelBuilder.Entity<ConstructionDepartment>()
                .HasMany(d => d.Subcategories)
                .WithOne(s => s.Department)
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConstructionDepartment>()
                .HasMany(d => d.Codes)
                .WithOne(c => c.Department)
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DepartmentSubcategory>()
                .HasMany(s => s.Codes)
                .WithOne(c => c.Subcategory)
                .HasForeignKey(c => c.SubcategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ConstructionCode>()
                .HasIndex(c => c.FullCode)
                .IsUnique();

            modelBuilder.Entity<ConstructionCode>()
                .HasIndex(c => new { c.DeptCode, c.CostTypeCode, c.SubCode });

            modelBuilder.Entity<ConstructionDepartment>()
                .HasIndex(d => d.DeptCode)
                .IsUnique();

            modelBuilder.Entity<DepartmentSubcategory>()
                .HasIndex(s => new { s.DeptCode, s.SubCode })
                .IsUnique();

            modelBuilder.Entity<CostType>()
                .HasIndex(ct => ct.Code)
                .IsUnique();

            modelBuilder.Entity<CodeSyncLog>()
                .HasOne(l => l.ConstructionCode)
                .WithMany()
                .HasForeignKey(l => l.ConstructionCodeId)
                .OnDelete(DeleteBehavior.Cascade);
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