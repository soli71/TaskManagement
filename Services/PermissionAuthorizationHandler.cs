using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Services
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var user = context.User;
            
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                return Task.CompletedTask;
            }

            // دریافت نقش کاربر
            var userRole = GetUserHighestRole(user);
            
            if (string.IsNullOrEmpty(userRole))
            {
                return Task.CompletedTask;
            }

            // بررسی دسترسی
            if (Permissions.HasPermission(userRole, requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        private string GetUserHighestRole(ClaimsPrincipal user)
        {
            // بررسی نقش‌ها بر اساس اولویت (از بالا به پایین)
            if (user.IsInRole(Roles.SystemAdmin))
                return Roles.SystemAdmin;
            
            if (user.IsInRole(Roles.CompanyManager))
                return Roles.CompanyManager;
            
            if (user.IsInRole(Roles.ProjectManager))
                return Roles.ProjectManager;
            
            if (user.IsInRole(Roles.Employee))
                return Roles.Employee;

            return string.Empty;
        }
    }
}
