using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    /// <summary>
    /// Target user types for PROVIA.
    /// Each trade type determines permit authority, required licenses, and dashboard workflow.
    /// </summary>
    public enum TradeType
    {
        GeneralContractor = 0,   // CSL - pulls building permits (structural, framing)
        Electrician = 1,          // MA Licensed Electrician - pulls electrical permits only
        Plumber = 2,              // Licensed Master/Journeyman Plumber - pulls plumbing permits only
        GasFitter = 3,            // Licensed Gas Fitter - pulls gas permits only
        SheetMetal = 4,           // Licensed Sheet Metal Worker - pulls HVAC/ductwork permits only
        FireProtection = 5,       // S-License / Sprinkler Fitter - pulls fire protection permits only
        PropertyInvestor = 6      // No permit authority - hires all trades, manages budget & timeline
    }

    /// <summary>
    /// Maps each trade type to the permits they are legally authorized to pull in Massachusetts.
    /// Specialty trade permits cannot be pulled by a GC or property owner.
    /// </summary>
    public static class PermitAuthority
    {
        public static readonly Dictionary<TradeType, string[]> CanPullPermits = new()
        {
            { TradeType.GeneralContractor, new[] { "BUILDING", "DEMOLITION", "FOUNDATION", "FRAMING" } },
            { TradeType.Electrician,       new[] { "ELECTRICAL" } },
            { TradeType.Plumber,           new[] { "PLUMBING" } },
            { TradeType.GasFitter,         new[] { "GAS" } },
            { TradeType.SheetMetal,        new[] { "SHEET_METAL", "HVAC" } },
            { TradeType.FireProtection,    new[] { "FIRE_PROTECTION", "SPRINKLER", "ALARM" } },
            { TradeType.PropertyInvestor,  new string[] { } } // Hires licensed contractors - no direct permit authority
        };

        public static readonly Dictionary<TradeType, string> TradeLabel = new()
        {
            { TradeType.GeneralContractor, "General Contractor" },
            { TradeType.Electrician,       "Electrician" },
            { TradeType.Plumber,           "Plumber" },
            { TradeType.GasFitter,         "Gas Fitter" },
            { TradeType.SheetMetal,        "Sheet Metal / HVAC" },
            { TradeType.FireProtection,    "Fire Protection / Sprinkler" },
            { TradeType.PropertyInvestor,  "Property Investor / Flipper" }
        };

        public static readonly Dictionary<TradeType, string> MaLicenseRequired = new()
        {
            { TradeType.GeneralContractor, "Construction Supervisor License (CSL)" },
            { TradeType.Electrician,       "MA Licensed Master or Journeyman Electrician" },
            { TradeType.Plumber,           "MA Licensed Master or Journeyman Plumber" },
            { TradeType.GasFitter,         "MA Licensed Gas Fitter" },
            { TradeType.SheetMetal,        "MA Licensed Sheet Metal Worker" },
            { TradeType.FireProtection,    "MA S-License (Sprinkler / Fire Protection)" },
            { TradeType.PropertyInvestor,  "No trade license required" }
        };
    }

    public class Company
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string AccountNumber { get; set; } = string.Empty; // Unique company identifier

        /// <summary>
        /// The type of contractor or business this company is.
        /// Determines permit authority, required credentials, and workflow configuration.
        /// </summary>
        public TradeType TradeType { get; set; } = TradeType.GeneralContractor;

        /// <summary>
        /// Primary trade license number (e.g. CSL number, Master Electrician license number)
        /// </summary>
        [StringLength(100)]
        public string? LicenseNumber { get; set; }

        /// <summary>
        /// Name of the license holder on the primary trade license
        /// </summary>
        [StringLength(200)]
        public string? LicenseHolderName { get; set; }

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(200)]
        public string ContactEmail { get; set; } = string.Empty;

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(10)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? ZipCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; } = "United States";

        public SubscriptionType SubscriptionType { get; set; } = SubscriptionType.Trial;
        
        public DateTime SubscriptionStartDate { get; set; } = DateTime.UtcNow;
        public DateTime? SubscriptionEndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsTrialExpired => SubscriptionType == SubscriptionType.Trial && 
                                     SubscriptionEndDate.HasValue && 
                                     SubscriptionEndDate.Value < DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Job> Jobs { get; set; } = new List<Job>();

        // License limits
        public int MaxUsers { get; set; } = 5; // Default for trial
        public int MaxJobs { get; set; } = 10; // Default for trial
        public bool CanUseAdvancedFeatures { get; set; } = false;

        // Company Branding
        [StringLength(500)]
        public string? LogoUrl { get; set; } // Path to company logo

        [StringLength(7)]
        public string PrimaryColor { get; set; } = "#FF9500"; // Orange default

        [StringLength(7)]
        public string SecondaryColor { get; set; } = "#2F5A7E"; // Teal default

        [StringLength(7)]
        public string? AccentColor { get; set; }

        [StringLength(200)]
        public string? Tagline { get; set; }

        [StringLength(500)]
        public string? WebsiteUrl { get; set; }

        public string GetDisplayName() => $"{CompanyName} ({AccountNumber})";
    }

    public enum SubscriptionType
    {
        Trial = 0,
        Basic = 1,
        Professional = 2,
        Enterprise = 3
    }

    public class CompanyRegistrationRequest
    {
        [Required]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string ContactEmail { get; set; } = string.Empty;

        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }

        [Required]
        public string AdminFirstName { get; set; } = string.Empty;

        [Required]
        public string AdminLastName { get; set; } = string.Empty;

        [Required]
        public string AdminPassword { get; set; } = string.Empty;

        public string LanguagePreference { get; set; } = "en";
        
        public SubscriptionType RequestedSubscription { get; set; } = SubscriptionType.Trial;

        public TradeType TradeType { get; set; } = TradeType.GeneralContractor;

        [StringLength(100)]
        public string? LicenseNumber { get; set; }

        [StringLength(200)]
        public string? LicenseHolderName { get; set; }
    }

    /// <summary>
    /// Company credentials - licenses and insurance documents
    /// </summary>
    public class CompanyCredential
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        [Required]
        [StringLength(50)]
        public string CredentialType { get; set; } = string.Empty; // CSL_LICENSE, HIC_LICENSE, GENERAL_LIABILITY, WORKERS_COMP

        [StringLength(100)]
        public string? LicenseNumber { get; set; }

        [StringLength(200)]
        public string? HolderName { get; set; }

        public DateTime? IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }

        [StringLength(200)]
        public string? InsuranceProvider { get; set; }

        [StringLength(100)]
        public string? PolicyNumber { get; set; }

        public decimal? CoverageAmount { get; set; }

        public string? DocumentPath { get; set; }

        [StringLength(255)]
        public string? OriginalFileName { get; set; }

        public long? FileSize { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, EXPIRED, PENDING_RENEWAL

        public string? Notes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public int? UploadedByUserId { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;
        public bool IsExpiringSoon => ExpirationDate.HasValue && 
                                      ExpirationDate.Value > DateTime.UtcNow && 
                                      ExpirationDate.Value < DateTime.UtcNow.AddDays(30);
    }

    public static class CredentialTypes
    {
        // General Contractor
        public const string CSL_LICENSE = "CSL_LICENSE";               // Construction Supervisor License (MA)
        public const string HIC_LICENSE = "HIC_LICENSE";               // Home Improvement Contractor (MA)

        // Electrician
        public const string MASTER_ELECTRICIAN = "MASTER_ELECTRICIAN"; // MA Licensed Master Electrician
        public const string JOURNEYMAN_ELECTRICIAN = "JOURNEYMAN_ELECTRICIAN";

        // Plumber
        public const string MASTER_PLUMBER = "MASTER_PLUMBER";         // MA Licensed Master Plumber
        public const string JOURNEYMAN_PLUMBER = "JOURNEYMAN_PLUMBER";

        // Gas Fitter
        public const string GAS_FITTER_LICENSE = "GAS_FITTER_LICENSE"; // MA Licensed Gas Fitter

        // Sheet Metal
        public const string SHEET_METAL_LICENSE = "SHEET_METAL_LICENSE"; // MA Licensed Sheet Metal Worker

        // Fire Protection
        public const string S_LICENSE = "S_LICENSE";                   // MA S-License (Sprinkler/Alarm)
        public const string SPRINKLER_FITTER = "SPRINKLER_FITTER";    // Licensed Sprinkler Fitter

        // All companies (regardless of trade)
        public const string GENERAL_LIABILITY = "GENERAL_LIABILITY";
        public const string WORKERS_COMP = "WORKERS_COMP";

        // All credential types across all trades (used for status summaries)
        public static readonly string[] All = {
            CSL_LICENSE, HIC_LICENSE,
            MASTER_ELECTRICIAN, JOURNEYMAN_ELECTRICIAN,
            MASTER_PLUMBER, JOURNEYMAN_PLUMBER,
            GAS_FITTER_LICENSE, SHEET_METAL_LICENSE,
            S_LICENSE, SPRINKLER_FITTER,
            GENERAL_LIABILITY, WORKERS_COMP
        };

        // Returns the required credential types for a given trade
        public static string[] ForTrade(TradeType trade) => trade switch
        {
            TradeType.GeneralContractor  => new[] { CSL_LICENSE, HIC_LICENSE, GENERAL_LIABILITY, WORKERS_COMP },
            TradeType.Electrician        => new[] { MASTER_ELECTRICIAN, JOURNEYMAN_ELECTRICIAN, GENERAL_LIABILITY, WORKERS_COMP },
            TradeType.Plumber            => new[] { MASTER_PLUMBER, JOURNEYMAN_PLUMBER, GENERAL_LIABILITY, WORKERS_COMP },
            TradeType.GasFitter          => new[] { GAS_FITTER_LICENSE, GENERAL_LIABILITY, WORKERS_COMP },
            TradeType.SheetMetal         => new[] { SHEET_METAL_LICENSE, GENERAL_LIABILITY, WORKERS_COMP },
            TradeType.FireProtection     => new[] { S_LICENSE, SPRINKLER_FITTER, GENERAL_LIABILITY, WORKERS_COMP },
            TradeType.PropertyInvestor   => new[] { GENERAL_LIABILITY },
            _                            => new[] { GENERAL_LIABILITY, WORKERS_COMP }
        };
    }
}