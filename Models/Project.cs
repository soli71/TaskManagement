using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementMvc.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "نام پروژه الزامی است")]
        [Display(Name = "نام پروژه")]
        [StringLength(200, ErrorMessage = "نام پروژه نمی‌تواند بیشتر از 200 کاراکتر باشد")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "توضیحات")]
        [StringLength(1000, ErrorMessage = "توضیحات نمی‌تواند بیشتر از 1000 کاراکتر باشد")]
        public string? Description { get; set; }

        [Display(Name = "کد پروژه")]
        [StringLength(50, ErrorMessage = "کد پروژه نمی‌تواند بیشتر از 50 کاراکتر باشد")]
        public string? Code { get; set; }

        [Display(Name = "تاریخ شروع")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "تاریخ پایان پیش‌بینی شده")]
        [DataType(DataType.Date)]
        public DateTime? EstimatedEndDate { get; set; }

        [Display(Name = "تاریخ پایان واقعی")]
        [DataType(DataType.Date)]
        public DateTime? ActualEndDate { get; set; }

        [Display(Name = "بودجه")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Budget { get; set; }

        [Display(Name = "هزینه واقعی")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ActualCost { get; set; }

        [Display(Name = "اولویت")]
        public ProjectPriority Priority { get; set; } = ProjectPriority.Medium;

        [Display(Name = "وضعیت")]
        public ProjectStatus? Status { get; set; }

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

        // Foreign Keys
        [Required(ErrorMessage = "انتخاب شرکت الزامی است")]
        [Display(Name = "شرکت")]
        public int CompanyId { get; set; }

        [Display(Name = "مدیر پروژه")]
        public int? ProjectManagerId { get; set; }

        // Navigation Properties
        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("ProjectManagerId")]
        public virtual ApplicationUser? ProjectManager { get; set; }

        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<ProjectAccess> ProjectAccess { get; set; } = new List<ProjectAccess>();
    }

    public enum ProjectPriority
    {
        [Display(Name = "کم")]
        Low = 1,
        [Display(Name = "متوسط")]
        Medium = 2,
        [Display(Name = "زیاد")]
        High = 3,
        [Display(Name = "بحرانی")]
        Critical = 4
    }

    public enum ProjectStatus
    {
        [Display(Name = "فعال")]
        Active = 1,
        [Display(Name = "متوقف شده")]
        OnHold = 2,
        [Display(Name = "تکمیل شده")]
        Completed = 3,
        [Display(Name = "لغو شده")]
        Cancelled = 4
    }
}
