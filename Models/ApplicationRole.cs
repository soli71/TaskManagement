using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TaskManagementMvc.Models
{
    public class ApplicationRole : IdentityRole<int>
    {
        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاریخ ایجاد")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "آخرین ویرایش")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "ویرایش شده توسط")]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
