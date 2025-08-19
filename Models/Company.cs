using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementMvc.Models
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "نام شرکت الزامی است")]
        [Display(Name = "نام شرکت")]
        [StringLength(200, ErrorMessage = "نام شرکت نمی‌تواند بیشتر از 200 کاراکتر باشد")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "توضیحات")]
        [StringLength(1000, ErrorMessage = "توضیحات نمی‌تواند بیشتر از 1000 کاراکتر باشد")]
        public string? Description { get; set; }

        [Display(Name = "آدرس")]
        [StringLength(500, ErrorMessage = "آدرس نمی‌تواند بیشتر از 500 کاراکتر باشد")]
        public string? Address { get; set; }

        [Display(Name = "شماره تماس")]
        [StringLength(20, ErrorMessage = "شماره تماس نمی‌تواند بیشتر از 20 کاراکتر باشد")]
        public string? Phone { get; set; }

        [Display(Name = "ایمیل")]
        [EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نیست")]
        [StringLength(100, ErrorMessage = "ایمیل نمی‌تواند بیشتر از 100 کاراکتر باشد")]
        public string? Email { get; set; }

        [Display(Name = "وب‌سایت")]
        [Url(ErrorMessage = "فرمت آدرس وب‌سایت صحیح نیست")]
        [StringLength(200, ErrorMessage = "آدرس وب‌سایت نمی‌تواند بیشتر از 200 کاراکتر باشد")]
        public string? Website { get; set; }

        [Display(Name = "لوگو")]
        [StringLength(500, ErrorMessage = "آدرس لوگو نمی‌تواند بیشتر از 500 کاراکتر باشد")]
        public string? LogoPath { get; set; }

        [Display(Name = "وضعیت")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاریخ ایجاد")]
    public DateTime CreatedAt { get; set; }

        [Display(Name = "تاریخ به‌روزرسانی")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "ایجاد شده توسط")]
        public string? CreatedBy { get; set; }

        [Display(Name = "به‌روزرسانی شده توسط")]
        public string? UpdatedBy { get; set; }

        // Navigation Properties
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
