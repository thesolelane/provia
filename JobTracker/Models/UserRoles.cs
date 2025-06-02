namespace JobTracker.Models
{
    public static class UserRoles
    {
        public const int Admin = 1510;
        public const int Supervisor = 1520;
        public const int FieldOperator = 2001;

        public static string GetRoleName(int roleCode)
        {
            return roleCode switch
            {
                Admin => "Admin",
                Supervisor => "Supervisor",
                FieldOperator => "Field Operator",
                _ => "Unknown"
            };
        }

        public static bool IsAdmin(int roleCode)
        {
            return roleCode == Admin || roleCode == Supervisor;
        }

        public static bool IsHighestAdmin(int roleCode)
        {
            return roleCode == Admin;
        }

        public static bool IsFieldOperator(int roleCode)
        {
            return roleCode == FieldOperator;
        }
    }
}