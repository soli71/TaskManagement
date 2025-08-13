using System.ComponentModel.DataAnnotations;

namespace TaskManagementMvc.Models
{
    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "نام دسترسی")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "کد دسترسی")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Display(Name = "گروه دسترسی")]
        public string? Group { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاریخ ایجاد")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
