namespace JobTracker.Models
{
    public static class UserRoles
    {
        public const int MasterAdmin = 1510;
        public const int Admin = 1520;
        public const int FieldOperator = 2001;

        public static string GetRoleName(int roleCode)
        {
            return roleCode switch
            {
                MasterAdmin => "Master Admin",
                Admin => "Admin",
                FieldOperator => "Field Operator",
                _ => "Unknown"
            };
        }

        public static bool IsAdmin(int roleCode)
        {
            return roleCode == MasterAdmin || roleCode == Admin;
        }

        public static bool IsMasterAdmin(int roleCode)
        {
            return roleCode == MasterAdmin;
        }

        public static bool IsFieldOperator(int roleCode)
        {
            return roleCode == FieldOperator;
        }
    }
}