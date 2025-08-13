using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementMvc.Models
{
    public class Performer
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "نام انجام‌دهنده الزامی است")]
        [Display(Name = "نام")]
        [StringLength(100, ErrorMessage = "نام نمی‌تواند بیشتر از 100 کاراکتر باشد")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "توضیحات")]
        [StringLength(500, ErrorMessage = "توضیحات نمی‌تواند بیشتر از 500 کاراکتر باشد")]
        public string? Description { get; set; }

        [Display(Name = "ایمیل")]
        [EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نیست")]
        [StringLength(100, ErrorMessage = "ایمیل نمی‌تواند بیشتر از 100 کاراکتر باشد")]
        public string? Email { get; set; }

        [Display(Name = "شماره تماس")]
        [StringLength(20, ErrorMessage = "شماره تماس نمی‌تواند بیشتر از 20 کاراکتر باشد")]
        public string? Phone { get; set; }

        [Display(Name = "وضعیت")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاریخ ایجاد")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "تاریخ به‌روزرسانی")]
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        [Display(Name = "شرکت")]
        public int? CompanyId { get; set; }

        [Display(Name = "سطح")]
        public int? GradeId { get; set; }

        // Navigation Properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("GradeId")]
        public virtual Grade? Grade { get; set; }

        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
