using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementMvc.Models
{
    public class UserRole
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public virtual ApplicationRole Role { get; set; } = null!;

        [Display(Name = "تاریخ تخصیص")]
    public DateTime AssignedAt { get; set; }

        [Display(Name = "تخصیص داده شده توسط")]
        public int? AssignedById { get; set; }

        [ForeignKey("AssignedById")]
        public virtual ApplicationUser? AssignedBy { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "یادداشت")]
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
