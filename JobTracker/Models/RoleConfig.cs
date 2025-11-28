using System;
using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    /// <summary>
    /// Role Configuration - Defines role codes, limits, and permissions
    /// 
    /// ROLE CODES (CONSOLIDATED):
    /// - 1510 = Admin (2 max per company) - Full system control
    /// - 1520 = Supervisor (4 max per company) - Project managers, create jobs, approve scopes
    /// - 1530 = Foreman (3 max per company) - On-site leads, assign daily tasks
    /// - 2001 = Field Operator (unlimited) - In-house crews under GC permit
    /// - 2010 = Subcontractor (unlimited) - Licensed trades with permit authority
    /// </summary>
    public static class RoleCodes
    {
        public const int Admin = 1510;
        public const int Supervisor = 1520;
        public const int Foreman = 1530;
        public const int FieldOperator = 2001;
        public const int Subcontractor = 2010;

        // Role limits per company
        public const int AdminLimit = 2;
        public const int SupervisorLimit = 4;
        public const int ForemanLimit = 3;
        public const int FieldOperatorLimit = -1; // Unlimited
        public const int SubcontractorLimit = -1; // Unlimited

        public static string GetRoleName(int roleCode)
        {
            return roleCode switch
            {
                1510 => "Admin",
                1520 => "Supervisor",
                1530 => "Foreman",
                2001 => "Field Operator",
                2010 => "Subcontractor",
                _ => "Unknown"
            };
        }

        public static int GetRoleLimit(int roleCode)
        {
            return roleCode switch
            {
                1510 => AdminLimit,
                1520 => SupervisorLimit,
                1530 => ForemanLimit,
                2001 => FieldOperatorLimit,
                2010 => SubcontractorLimit,
                _ => -1
            };
        }

        public static bool IsLimitedRole(int roleCode)
        {
            return roleCode == Admin || roleCode == Supervisor || roleCode == Foreman;
        }

        public static bool RequiresLicensedTrade(int roleCode)
        {
            return roleCode == Subcontractor;
        }

        public static bool CanHoldPermit(int roleCode)
        {
            return roleCode == Subcontractor;
        }

        public static bool WorksUnderGCPermit(int roleCode)
        {
            return roleCode == FieldOperator;
        }
    }

    /// <summary>
    /// Role Permissions - What each role can do
    /// </summary>
    public static class RolePermissions
    {
        // ADMIN (1510) - Full control
        public static readonly string[] AdminPermissions = new[]
        {
            "CREATE_JOB", "EDIT_JOB", "DELETE_JOB",
            "CREATE_USER", "EDIT_USER", "DELETE_USER",
            "EDIT_COMPANY_SETTINGS", "EDIT_JURISDICTION",
            "EDIT_CODE_RULES", "EDIT_TEMPLATES",
            "APPROVE_SCOPE", "APPROVE_QUOTE", "APPROVE_CHANGE_ORDER",
            "REQUEST_INSPECTION", "OVERRIDE_INSPECTION",
            "ASSIGN_TRADES", "VIEW_ALL_FINANCIALS",
            "VIEW_ALL_JOBS", "VIEW_ALL_TRADES"
        };

        // SUPERVISOR (1520) - Project management
        public static readonly string[] SupervisorPermissions = new[]
        {
            "CREATE_JOB", "EDIT_JOB",
            "APPROVE_SCOPE", "APPROVE_QUOTE", "APPROVE_CHANGE_ORDER",
            "REQUEST_INSPECTION", "TRACK_INSPECTION",
            "ASSIGN_FOREMAN", "ASSIGN_SUBS", "ASSIGN_FIELD_OPS",
            "VIEW_ALL_TRADES_ON_JOB", "VIEW_JOB_PERMITS"
        };

        // FOREMAN (1530) - On-site leadership
        public static readonly string[] ForemanPermissions = new[]
        {
            "VIEW_JOB_TASKS", "VIEW_JOB_TIMELINE",
            "ASSIGN_DAILY_TASKS", "MARK_WORK_COMPLETE",
            "MARK_READY_FOR_INSPECTION", "UPLOAD_PHOTOS",
            "UPLOAD_DAILY_LOGS", "START_CHECKLISTS",
            "VIEW_ALL_TRADES_ON_ASSIGNED_JOB"
        };

        // SUBCONTRACTOR (2010) - Licensed trades
        public static readonly string[] SubcontractorPermissions = new[]
        {
            "VIEW_ASSIGNED_JOB", "VIEW_OWN_TRADE_SCOPE",
            "UPLOAD_SCOPE_DETAILS", "UPLOAD_MATERIAL_LISTS",
            "MARK_TASKS_COMPLETE", "FLAG_NEED_INSPECTION",
            "FLAG_NEED_CHANGE_ORDER", "VIEW_OWN_PERMIT_STATUS"
        };

        // FIELD OPERATOR (2001) - In-house crews
        public static readonly string[] FieldOperatorPermissions = new[]
        {
            "VIEW_ASSIGNED_TASKS", "VIEW_TASK_DRAWINGS",
            "UPDATE_TASK_STATUS", "UPLOAD_PHOTOS",
            "COMPLETE_CHECKLISTS"
        };

        public static string[] GetPermissions(int roleCode)
        {
            return roleCode switch
            {
                1510 => AdminPermissions,
                1520 => SupervisorPermissions,
                1530 => ForemanPermissions,
                2001 => FieldOperatorPermissions,
                2010 => SubcontractorPermissions,
                _ => Array.Empty<string>()
            };
        }

        public static bool HasPermission(int roleCode, string permission)
        {
            var permissions = GetPermissions(roleCode);
            return Array.Exists(permissions, p => p == permission);
        }
    }

    /// <summary>
    /// Department Codes - For filtering views
    /// </summary>
    public static class DepartmentCodes
    {
        public const string Building = "BUILDING";
        public const string Electrical = "ELECTRICAL";
        public const string Plumbing = "PLUMBING";
        public const string Mechanical = "MECHANICAL";
        public const string Fire = "FIRE";
        public const string Accessibility = "ACCESSIBILITY";

        public static readonly string[] AllDepartments = new[]
        {
            Building, Electrical, Plumbing, Mechanical, Fire, Accessibility
        };
    }

    /// <summary>
    /// Trade Types - For categorizing subcontractors
    /// </summary>
    public static class TradeTypes
    {
        // Licensed trades (require permit authority)
        public const string Plumber = "PLUMBER";
        public const string Electrician = "ELECTRICIAN";
        public const string HVAC = "HVAC";
        public const string SheetMetal = "SHEET_METAL";
        public const string GasFitter = "GAS_FITTER";
        public const string FireProtection = "FIRE_PROTECTION";

        // General trades (work under GC permit)
        public const string Roofing = "ROOFING";
        public const string Siding = "SIDING";
        public const string Flooring = "FLOORING";
        public const string Tile = "TILE";
        public const string Painting = "PAINTING";
        public const string Drywall = "DRYWALL";
        public const string Framing = "FRAMING";
        public const string Masonry = "MASONRY";
        public const string Demo = "DEMO";
        public const string Windows = "WINDOWS";
        public const string FinishCarpentry = "FINISH_CARPENTRY";
        public const string Cabinets = "CABINETS";

        public static readonly string[] LicensedTrades = new[]
        {
            Plumber, Electrician, HVAC, SheetMetal, GasFitter, FireProtection
        };

        public static readonly string[] GeneralTrades = new[]
        {
            Roofing, Siding, Flooring, Tile, Painting, Drywall,
            Framing, Masonry, Demo, Windows, FinishCarpentry, Cabinets
        };

        public static bool IsLicensedTrade(string tradeType)
        {
            return Array.Exists(LicensedTrades, t => t == tradeType);
        }
    }
}
