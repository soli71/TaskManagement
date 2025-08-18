using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementMvc.Models
{
    /// <summary>
    /// تعریف زمان‌بندی صدور خودکار فاکتور از روی تسک‌های تکمیل شده یک شرکت.
    /// هر زمان‌بندی فقط برای یک شرکت (CompanyId) اعمال می‌شود.
    /// </summary>
    public class InvoiceSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "عنوان زمان‌بندی")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "شرکت")]
        public int? CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public virtual Company? Company { get; set; }

        [Display(Name = "نوع تناوب")]
        public InvoiceSchedulePeriodType PeriodType { get; set; } = InvoiceSchedulePeriodType.Daily;

        [Display(Name = "روز هفته (برای هفتگی)")]
        public DayOfWeek? DayOfWeek { get; set; }

        [Range(1, 31)]
        [Display(Name = "روز ماه (برای ماهانه)")]
        public int? DayOfMonth { get; set; }

        [Range(0, 23)]
        [Display(Name = "ساعت اجرا (۰-۲۳)")]
        public int HourOfDay { get; set; } = 6; // پیش‌فرض ۶ صبح

        [MaxLength(1000)]
        [Display(Name = "ایمیل‌های گیرندگان (جدا شده با ; یا خط جدید)")]
        public string? RecipientEmails { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "آخرین اجرا")]
        public DateTime? LastRunAt { get; set; }

        [Display(Name = "اجرای بعدی")]
        public DateTime? NextRunAt { get; set; }

        [Display(Name = "توضیحات")]
        [MaxLength(500)]
        public string? Description { get; set; }

        public virtual ICollection<InvoiceJobRunLog> RunLogs { get; set; } = new List<InvoiceJobRunLog>();
    }

    public enum InvoiceSchedulePeriodType
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3
    }
}
