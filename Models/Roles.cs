namespace TaskManagementMvc.Models
{
    public static class Roles
    {
        // نقش‌ها با نام‌های انگلیسی برای سیستم
        public const string SystemAdmin = "SystemAdmin";
        public const string CompanyManager = "CompanyManager";
        public const string ProjectManager = "ProjectManager";
        public const string Employee = "Employee";

        // نام‌های فارسی نقش‌ها
        public static readonly Dictionary<string, string> RoleDisplayNames = new Dictionary<string, string>
        {
            { SystemAdmin, "مدیر سیستم" },
            { CompanyManager, "مدیر شرکت" },
            { ProjectManager, "مدیر پروژه" },
            { Employee, "کارمند" }
        };

        // سطح نقش‌ها (عدد بالاتر = اختیار بیشتر)
        public static readonly Dictionary<string, int> RoleLevels = new Dictionary<string, int>
        {
            { SystemAdmin, 4 },      // مدیر سیستم - بالاترین سطح
            { CompanyManager, 3 },   // مدیر شرکت
            { ProjectManager, 2 },   // مدیر پروژه
            { Employee, 1 }          // کارمند - پایین‌ترین سطح
        };

        /// <summary>
        /// دریافت نام فارسی نقش
        /// </summary>
        public static string GetDisplayName(string role)
        {
            return RoleDisplayNames.TryGetValue(role, out var displayName) ? displayName : role;
        }

        /// <summary>
        /// دریافت سطح نقش
        /// </summary>
        public static int GetRoleLevel(string role)
        {
            return RoleLevels.TryGetValue(role, out var level) ? level : 0;
        }

        /// <summary>
        /// بررسی اینکه آیا کاربر می‌تواند نقش مقصد را تخصیص دهد
        /// </summary>
        public static bool CanAssignRole(string userRole, string targetRole)
        {
            var userLevel = GetRoleLevel(userRole);
            var targetLevel = GetRoleLevel(targetRole);
            
            // کاربر می‌تواند نقش‌هایی با سطح پایین‌تر از خود تخصیص دهد
            return userLevel > targetLevel;
        }

        /// <summary>
        /// دریافت نقش‌هایی که کاربر می‌تواند تخصیص دهد
        /// </summary>
        public static List<string> GetAssignableRoles(string userRole)
        {
            var userLevel = GetRoleLevel(userRole);
            return RoleLevels
                .Where(kvp => kvp.Value < userLevel)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// دریافت نقش‌هایی که کاربر می‌تواند مدیریت کند (مشاهده و ویرایش)
        /// </summary>
        public static List<string> GetManageableRoles(string userRole)
        {
            var userLevel = GetRoleLevel(userRole);
            return RoleLevels
                .Where(kvp => kvp.Value <= userLevel)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }
}
