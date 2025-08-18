using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Services
{
    public class ProjectAccessService
    {
        private readonly TaskManagementContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectAccessService(TaskManagementContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// اعمال دسترسی خودکار برای مدیران هنگام ایجاد پروژه
        /// </summary>
        public async Task GrantManagerAccessOnProjectCreation(int projectId, int companyId, int? createdById = null)
        {
            // یافتن تمام مدیران شرکت
            var managers = await GetCompanyManagers(companyId);

            foreach (var manager in managers)
            {
                // بررسی اینکه آیا قبلاً دسترسی دارد یا نه
                var existingAccess = await _context.ProjectAccess
                    .FirstOrDefaultAsync(pa => pa.ProjectId == projectId && pa.UserId == manager.Id);

                if (existingAccess == null)
                {
                    var projectAccess = new ProjectAccess
                    {
                        ProjectId = projectId,
                        UserId = manager.Id,
                        GrantedAt = DateTime.UtcNow,
                        GrantedById = createdById,
                        IsActive = true,
                        Reason = "دسترسی خودکار برای مدیر شرکت"
                    };

                    _context.ProjectAccess.Add(projectAccess);
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// اعمال دسترسی به تمام پروژه‌های شرکت هنگام تعیین کاربر به عنوان مدیر
        /// </summary>
        public async Task GrantAllCompanyProjectsAccessToManager(int userId, int companyId, int? grantedById = null)
        {
            // یافتن تمام پروژه‌های شرکت
            var companyProjects = await _context.Projects
                .Where(p => p.CompanyId == companyId && p.IsActive)
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var projectId in companyProjects)
            {
                // بررسی اینکه آیا قبلاً دسترسی دارد یا نه
                var existingAccess = await _context.ProjectAccess
                    .FirstOrDefaultAsync(pa => pa.ProjectId == projectId && pa.UserId == userId);

                if (existingAccess == null)
                {
                    var projectAccess = new ProjectAccess
                    {
                        ProjectId = projectId,
                        UserId = userId,
                        GrantedAt = DateTime.UtcNow,
                        GrantedById = grantedById,
                        IsActive = true,
                        Reason = "دسترسی خودکار به علت تعیین به عنوان مدیر شرکت"
                    };

                    _context.ProjectAccess.Add(projectAccess);
                }
                else if (!existingAccess.IsActive)
                {
                    // اگر دسترسی قبلاً وجود داشته ولی غیرفعال بوده، آن را فعال کن
                    existingAccess.IsActive = true;
                    existingAccess.GrantedAt = DateTime.UtcNow;
                    existingAccess.GrantedById = grantedById;
                    existingAccess.Reason = "دسترسی خودکار به علت تعیین به عنوان مدیر شرکت";
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// حذف دسترسی از تمام پروژه‌های شرکت هنگام حذف نقش مدیر از کاربر
        /// </summary>
        public async Task RevokeAllCompanyProjectsAccessFromUser(int userId, int companyId, int? revokedById = null)
        {
            // یافتن تمام دسترسی‌های کاربر به پروژه‌های شرکت
            var userProjectAccess = await _context.ProjectAccess
                .Include(pa => pa.Project)
                .Where(pa => pa.UserId == userId &&
                            pa.Project.CompanyId == companyId &&
                            pa.IsActive)
                .ToListAsync();

            foreach (var access in userProjectAccess)
            {
                access.IsActive = false;
                access.RevokedAt = DateTime.UtcNow;
                access.RevokedById = revokedById;
                access.RevokeReason = "حذف دسترسی به علت حذف نقش مدیر";
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// یافتن تمام مدیران یک شرکت
        /// </summary>
        private async Task<List<ApplicationUser>> GetCompanyManagers(int companyId)
        {
            var managerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.CompanyManager);
            if (managerRole == null) return new List<ApplicationUser>();

            var managers = await _context.Users
                .Include(u => u.Company)
                .Where(u => u.CompanyId == companyId &&
                           u.IsActive &&
                           _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == managerRole.Id))
                .ToListAsync();

            return managers;
        }

        /// <summary>
        /// بررسی اینکه آیا کاربر مدیر شرکت است یا نه
        /// </summary>
        public async Task<bool> IsUserManagerOfCompany(int userId, int companyId)
        {
            var managerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "CompanyManager");
            if (managerRole == null) return false;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId &&
                                        u.CompanyId == companyId &&
                                        u.IsActive);

            if (user == null) return false;

            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == managerRole.Id);
        }
    }
}