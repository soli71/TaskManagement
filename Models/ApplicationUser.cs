using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TaskManagementMvc.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        [Display(Name = "نام کامل")]
        [StringLength(100, ErrorMessage = "نام کامل نمی‌تواند بیشتر از 100 کاراکتر باشد")]
        public string? FullName { get; set; }

        [Display(Name = "وضعیت")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاریخ ایجاد")]
    public DateTime CreatedAt { get; set; }

        [Display(Name = "آخرین ورود")]
        public DateTime? LastLoginAt { get; set; }

        [Display(Name = "یادداشت")]
        [StringLength(500, ErrorMessage = "یادداشت نمی‌تواند بیشتر از 500 کاراکتر باشد")]
        public string? Notes { get; set; }

        // Company relationship
        [Display(Name = "شرکت")]
        public int? CompanyId { get; set; }

        [Display(Name = "نقش در شرکت")]
        public CompanyRole CompanyRole { get; set; } = CompanyRole.User;

        // Grade relationship
        [Display(Name = "رتبه")]
        public int? GradeId { get; set; }

        [Display(Name = "شماره شبا")]
        [StringLength(26, ErrorMessage = "شماره شبا نمی‌تواند بیشتر از 26 کاراکتر باشد")]
        public string? IbanNumber { get; set; }

        [Display(Name = "شماره کارت")]
        [StringLength(16, ErrorMessage = "شماره کارت نمی‌تواند بیشتر از 16 کاراکتر باشد")]
        public string? CardNumber { get; set; }

        // Computed properties for compatibility
        public string? Name => FullName;

        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual Grade? Grade { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
        public virtual ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
        public virtual ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
        public virtual ICollection<Invoice> CreatedInvoices { get; set; } = new List<Invoice>();
        public virtual ICollection<TaskHistory> TaskHistories { get; set; } = new List<TaskHistory>();
        public virtual ICollection<TaskAttachment> TaskAttachments { get; set; } = new List<TaskAttachment>();
    }

    public enum CompanyRole
    {
        [Display(Name = "کاربر عادی")]
        User = 1,
        [Display(Name = "مدیر")]
        Manager = 2,
        [Display(Name = "ادمین شرکت")]
        CompanyAdmin = 3
    }
}
