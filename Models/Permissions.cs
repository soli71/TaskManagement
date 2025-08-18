using System.Collections.Generic;

namespace TaskManagementMvc.Models
{
    public static class Permissions
    {
        // System Management - فقط برای SystemAdmin
        public const string ManageSystem = "ManageSystem";
        public const string ViewSystemLogs = "ViewSystemLogs";
        public const string ManageAllCompanies = "ManageAllCompanies";
        public const string ManageAllUsers = "ManageAllUsers";
        public const string ManageSystemRoles = "ManageSystemRoles";

        // Company Management - SystemAdmin و CompanyManager
        public const string ManageCompany = "ManageCompany";
        public const string ViewCompany = "ViewCompany";
        public const string ManageCompanyUsers = "ManageCompanyUsers";
        public const string ViewCompanyUsers = "ViewCompanyUsers";
        public const string ManageCompanyProjects = "ManageCompanyProjects";
        public const string ViewCompanyProjects = "ViewCompanyProjects";

        // Project Management - SystemAdmin، CompanyManager و ProjectManager
        public const string ManageProjects = "ManageProjects";
        public const string ViewProjects = "ViewProjects";
        public const string CreateProjects = "CreateProjects";
        public const string EditProjects = "EditProjects";
        public const string DeleteProjects = "DeleteProjects";
        public const string ManageProjectAccess = "ManageProjectAccess";
        public const string ViewProjectAccess = "ViewProjectAccess";

        // Task Management - همه نقش‌ها با محدودیت
        public const string ManageTasks = "ManageTasks";
        public const string ViewTasks = "ViewTasks";
        public const string CreateTasks = "CreateTasks";
        public const string EditTasks = "EditTasks";
        public const string DeleteTasks = "DeleteTasks";
        public const string ArchiveTasks = "ArchiveTasks";
        public const string ChangeTaskStatus = "ChangeTaskStatus";
        public const string ViewArchivedTasks = "ViewArchivedTasks";
        public const string ManageTaskAttachments = "ManageTaskAttachments";

        // User Management - بر اساس سطح نقش
        public const string ViewUsers = "ViewUsers";
        public const string CreateUsers = "CreateUsers";
        public const string EditUsers = "EditUsers";
        public const string DeleteUsers = "DeleteUsers";
        public const string ChangeUserPassword = "ChangeUserPassword";
        public const string ManageUserRoles = "ManageUserRoles";
        public const string ViewUserDetails = "ViewUserDetails";

        // Role Management - SystemAdmin و CompanyManager
        public const string ViewRoles = "ViewRoles";
        public const string CreateRoles = "CreateRoles";
        public const string EditRoles = "EditRoles";
        public const string DeleteRoles = "DeleteRoles";

        // Grade Management - SystemAdmin و CompanyManager
        public const string ManageGrades = "ManageGrades";
        public const string ViewGrades = "ViewGrades";

        // Invoice Management - SystemAdmin، CompanyManager و ProjectManager
        public const string ManageInvoices = "ManageInvoices";
        public const string ViewInvoices = "ViewInvoices";
        public const string CreateInvoices = "CreateInvoices";
        public const string EditInvoices = "EditInvoices";

        // Email Template Management - SystemAdmin و CompanyManager
        public const string ManageEmailTemplates = "ManageEmailTemplates";
        public const string ViewEmailTemplates = "ViewEmailTemplates";

        // نقشه دسترسی‌ها بر اساس سطح نقش
        public static readonly Dictionary<string, List<string>> RolePermissions = new()
        {
            [Roles.SystemAdmin] = new List<string>
            {
                // تمام دسترسی‌ها
                ManageSystem, ViewSystemLogs, ManageAllCompanies, ManageAllUsers, ManageSystemRoles,
                ManageCompany, ViewCompany, ManageCompanyUsers, ViewCompanyUsers, ManageCompanyProjects, ViewCompanyProjects,
                ManageProjects, ViewProjects, CreateProjects, EditProjects, DeleteProjects, ManageProjectAccess, ViewProjectAccess,
                ManageTasks, ViewTasks, CreateTasks, EditTasks, DeleteTasks, ArchiveTasks, ChangeTaskStatus, ViewArchivedTasks, ManageTaskAttachments,
                ViewUsers, CreateUsers, EditUsers, DeleteUsers, ChangeUserPassword, ManageUserRoles, ViewUserDetails,
                ViewRoles, CreateRoles, EditRoles, DeleteRoles,
                ManageGrades, ViewGrades,
                ManageInvoices, ViewInvoices, CreateInvoices, EditInvoices,
                ManageEmailTemplates, ViewEmailTemplates
            },

            [Roles.CompanyManager] = new List<string>
            {
                // مدیریت شرکت و زیرمجموعه‌ها
                ManageCompany, ViewCompany, ManageCompanyUsers, ViewCompanyUsers, ManageCompanyProjects, ViewCompanyProjects,
                ManageProjects, ViewProjects, CreateProjects, EditProjects, DeleteProjects, ManageProjectAccess, ViewProjectAccess,
                ManageTasks, ViewTasks, CreateTasks, EditTasks, DeleteTasks, ArchiveTasks, ChangeTaskStatus, ViewArchivedTasks, ManageTaskAttachments,
                ViewUsers, CreateUsers, EditUsers, DeleteUsers, ChangeUserPassword, ManageUserRoles, ViewUserDetails,
                ViewRoles, CreateRoles, EditRoles, DeleteRoles,
                ManageGrades, ViewGrades,
                ManageInvoices, ViewInvoices, CreateInvoices, EditInvoices,
                ManageEmailTemplates, ViewEmailTemplates
            },

            [Roles.ProjectManager] = new List<string>
            {
                // مدیریت پروژه‌ها و وظایف
                ViewCompany, ViewCompanyUsers, ViewCompanyProjects,
                ManageProjects, ViewProjects, CreateProjects, EditProjects, ManageProjectAccess, ViewProjectAccess,
                ManageTasks, ViewTasks, CreateTasks, EditTasks, DeleteTasks, ArchiveTasks, ChangeTaskStatus, ViewArchivedTasks, ManageTaskAttachments,
                ViewUsers, ViewUserDetails,
                ViewRoles, ViewGrades,
                ManageInvoices, ViewInvoices, CreateInvoices, EditInvoices
            },

            [Roles.Employee] = new List<string>
            {
                // دسترسی‌های محدود
                ViewCompany, ViewCompanyProjects,
                ViewProjects, ViewProjectAccess,
                ViewTasks, CreateTasks, EditTasks, ChangeTaskStatus, ManageTaskAttachments,
                ViewUsers, ViewUserDetails,
                ViewRoles, ViewGrades,
                ViewInvoices
            }
        };

        // نام‌های فارسی برای دسترسی‌ها
        public static readonly Dictionary<string, string> PermissionDisplayNames = new()
        {
            [ManageSystem] = "مدیریت سیستم",
            [ViewSystemLogs] = "مشاهده لاگ‌های سیستم",
            [ManageAllCompanies] = "مدیریت تمام شرکت‌ها",
            [ManageAllUsers] = "مدیریت تمام کاربران",
            [ManageSystemRoles] = "مدیریت نقش‌های سیستم",

            [ManageCompany] = "مدیریت شرکت",
            [ViewCompany] = "مشاهده شرکت",
            [ManageCompanyUsers] = "مدیریت کاربران شرکت",
            [ViewCompanyUsers] = "مشاهده کاربران شرکت",
            [ManageCompanyProjects] = "مدیریت پروژه‌های شرکت",
            [ViewCompanyProjects] = "مشاهده پروژه‌های شرکت",

            [ManageProjects] = "مدیریت پروژه‌ها",
            [ViewProjects] = "مشاهده پروژه‌ها",
            [CreateProjects] = "ایجاد پروژه",
            [EditProjects] = "ویرایش پروژه",
            [DeleteProjects] = "حذف پروژه",
            [ManageProjectAccess] = "مدیریت دسترسی پروژه",
            [ViewProjectAccess] = "مشاهده دسترسی پروژه",

            [ManageTasks] = "مدیریت وظایف",
            [ViewTasks] = "مشاهده وظایف",
            [CreateTasks] = "ایجاد وظیفه",
            [EditTasks] = "ویرایش وظیفه",
            [DeleteTasks] = "حذف وظیفه",
            [ArchiveTasks] = "آرشیو وظایف",
            [ChangeTaskStatus] = "تغییر وضعیت وظیفه",
            [ViewArchivedTasks] = "مشاهده وظایف آرشیو شده",
            [ManageTaskAttachments] = "مدیریت ضمائم وظیفه",

            [ViewUsers] = "مشاهده کاربران",
            [CreateUsers] = "ایجاد کاربر",
            [EditUsers] = "ویرایش کاربر",
            [DeleteUsers] = "حذف کاربر",
            [ChangeUserPassword] = "تغییر رمز عبور کاربر",
            [ManageUserRoles] = "مدیریت نقش‌های کاربر",
            [ViewUserDetails] = "مشاهده جزئیات کاربر",

            [ViewRoles] = "مشاهده نقش‌ها",
            [CreateRoles] = "ایجاد نقش",
            [EditRoles] = "ویرایش نقش",
            [DeleteRoles] = "حذف نقش",

            [ManageGrades] = "مدیریت رتبه‌ها",
            [ViewGrades] = "مشاهده رتبه‌ها",

            [ManageInvoices] = "مدیریت فاکتورها",
            [ViewInvoices] = "مشاهده فاکتورها",
            [CreateInvoices] = "ایجاد فاکتور",
            [EditInvoices] = "ویرایش فاکتور",

            [ManageEmailTemplates] = "مدیریت قالب‌های ایمیل",
            [ViewEmailTemplates] = "مشاهده قالب‌های ایمیل"
        };

        // بررسی اینکه آیا نقش مشخصی دسترسی خاصی دارد یا نه
        public static bool HasPermission(string role, string permission)
        {
            return RolePermissions.ContainsKey(role) && RolePermissions[role].Contains(permission);
        }

        // دریافت تمام دسترسی‌های یک نقش
        public static List<string> GetRolePermissions(string role)
        {
            return RolePermissions.ContainsKey(role) ? RolePermissions[role] : new List<string>();
        }

        // دریافت نام فارسی دسترسی
        public static string GetDisplayName(string permission)
        {
            return PermissionDisplayNames.ContainsKey(permission) ? PermissionDisplayNames[permission] : permission;
        }

        // دریافت دسترسی‌هایی که نقش مشخصی می‌تواند به دیگران تخصیص دهد
        public static List<string> GetAssignablePermissions(string currentUserRole)
        {
            if (!RolePermissions.ContainsKey(currentUserRole))
                return new List<string>();

            var assignablePermissions = new List<string>();
            var currentRoleLevel = Roles.GetRoleLevel(currentUserRole);

            // نقش‌ها می‌توانند دسترسی‌هایی را تخصیص دهند که خودشان دارند
            // و فقط به نقش‌هایی با سطح پایین‌تر
            foreach (var permission in RolePermissions[currentUserRole])
            {
                assignablePermissions.Add(permission);
            }

            return assignablePermissions;
        }

        // بررسی اینکه آیا نقش فعلی می‌تواند دسترسی خاصی را به نقش هدف تخصیص دهد
        public static bool CanAssignPermission(string currentUserRole, string targetRole, string permission)
        {
            var currentRoleLevel = Roles.GetRoleLevel(currentUserRole);
            var targetRoleLevel = Roles.GetRoleLevel(targetRole);

            // فقط می‌توان به نقش‌های پایین‌تر دسترسی تخصیص داد
            if (currentRoleLevel <= targetRoleLevel)
                return false;

            // نقش فعلی باید خود این دسترسی را داشته باشد
            if (!HasPermission(currentUserRole, permission))
                return false;

            return true;
        }
    }
}
