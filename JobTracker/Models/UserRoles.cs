namespace JobTracker.Models
{
    public static class UserRoles
    {
        public const int Admin = 1510;
        public const int Foreman = 1520;
        public const int Supervisor = 1530;
        public const int FieldOperator = 2001;

        public static string GetRoleName(int roleCode)
        {
            return roleCode switch
            {
                Admin => "Admin",
                Foreman => "Foreman",
                Supervisor => "Supervisor",
                FieldOperator => "Field Operator",
                _ => "Unknown"
            };
        }

        public static bool IsAdmin(int roleCode)
        {
            return roleCode == Admin;
        }

        public static bool IsForeman(int roleCode)
        {
            return roleCode == Foreman;
        }

        public static bool IsSupervisor(int roleCode)
        {
            return roleCode == Supervisor;
        }

        public static bool IsFieldOperator(int roleCode)
        {
            return roleCode == FieldOperator;
        }

        public static bool IsManagement(int roleCode)
        {
            return roleCode == Admin || roleCode == Foreman || roleCode == Supervisor;
        }

        // Role limits per company
        public static int GetMaxRoleCount(int roleCode)
        {
            return roleCode switch
            {
                Admin => 2,
                Foreman => 5,
                Supervisor => int.MaxValue,
                FieldOperator => int.MaxValue,
                _ => 0
            };
        }
    }
}