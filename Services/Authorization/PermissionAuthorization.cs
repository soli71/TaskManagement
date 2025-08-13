using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Services.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string PermissionCode { get; }
        public PermissionRequirement(string permissionCode)
        {
            PermissionCode = permissionCode;
        }
    }

    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly TaskManagementContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public PermissionAuthorizationHandler(TaskManagementContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue))
            {
                return;
            }
            if (!int.TryParse(userIdValue, out var userId))
            {
                return;
            }

            // If user is in Admin role, grant immediately
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            // Check DB: user active, roles active, role-permission active, permission active and code matches
            var hasPermission = (from ur in _db.UserRoles
                                 join r in _db.Roles on ur.RoleId equals r.Id
                                 join rp in _db.RolePermissions on r.Id equals rp.RoleId
                                 join p in _db.Permissions on rp.PermissionId equals p.Id
                                 where ur.UserId == userId
                                       && ur.IsActive
                                       && r.NormalizedName != null
                                       && r.IsActive
                                       && rp.IsActive
                                       && p.IsActive
                                       && p.Code == requirement.PermissionCode
                                 select p.Id).Any();

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }

    // Dynamic policy provider: any policy name is treated as a permission code
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Create a policy that requires the permission named 'policyName'
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
    }
}
