using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Services.Authorization
{
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return _fallbackPolicyProvider.GetDefaultPolicyAsync();
        }

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            return _fallbackPolicyProvider.GetFallbackPolicyAsync();
        }

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // اگر policy نام یکی از permissions است، یک policy مخصوص آن ایجاد کن
            if (IsPermissionPolicy(policyName))
            {
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(policyName))
                    .Build();
                
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            // در غیر این صورت به fallback provider بسپار
            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        private bool IsPermissionPolicy(string policyName)
        {
            // بررسی اینکه آیا policy name یکی از permissions تعریف شده است
            return typeof(Permissions)
                .GetFields()
                .Where(f => f.IsStatic && f.IsLiteral && f.FieldType == typeof(string))
                .Any(f => f.GetValue(null)?.ToString() == policyName);
        }
    }
}
