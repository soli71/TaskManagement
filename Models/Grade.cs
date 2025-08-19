using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementMvc.Models
{
    public class Grade
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "نام سطح الزامی است")]
        [Display(Name = "نام")]
        [StringLength(100, ErrorMessage = "نام نمی‌تواند بیشتر از 100 کاراکتر باشد")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "توضیحات")]
        [StringLength(500, ErrorMessage = "توضیحات نمی‌تواند بیشتر از 500 کاراکتر باشد")]
        public string? Description { get; set; }

        [Display(Name = "نرخ ساعتی")]
        [Range(0, double.MaxValue, ErrorMessage = "نرخ ساعتی باید عدد مثبت باشد")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "وضعیت")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاریخ ایجاد")]
    public DateTime CreatedAt { get; set; }

        [Display(Name = "تاریخ به‌روزرسانی")]
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        [Display(Name = "شرکت")]
        public int? CompanyId { get; set; }

        // Navigation Properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
