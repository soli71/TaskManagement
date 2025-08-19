using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementMvc.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "شماره فاکتور الزامی است")]
        [Display(Name = "شماره فاکتور")]
        [StringLength(50, ErrorMessage = "شماره فاکتور نمی‌تواند بیشتر از 50 کاراکتر باشد")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاریخ فاکتور الزامی است")]
        [Display(Name = "تاریخ فاکتور")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Display(Name = "تاریخ سررسید")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "وضعیت")]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        [Display(Name = "توضیحات")]
        [StringLength(1000, ErrorMessage = "توضیحات نمی‌تواند بیشتر از 1000 کاراکتر باشد")]
        public string? Description { get; set; }

        [Display(Name = "ایمیل مشتری")]
        [EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نیست")]
        [StringLength(100, ErrorMessage = "ایمیل نمی‌تواند بیشتر از 100 کاراکتر باشد")]
        public string? CustomerEmail { get; set; }

        [Display(Name = "نام مشتری")]
        [StringLength(200, ErrorMessage = "نام مشتری نمی‌تواند بیشتر از 200 کاراکتر باشد")]
        public string? CustomerName { get; set; }

        [Display(Name = "آدرس مشتری")]
        [StringLength(500, ErrorMessage = "آدرس مشتری نمی‌تواند بیشتر از 500 کاراکتر باشد")]
        public string? CustomerAddress { get; set; }

        [Display(Name = "تاریخ ارسال ایمیل")]
        public DateTime? EmailSentAt { get; set; }

        [Display(Name = "تاریخ ایجاد")]
    public DateTime CreatedAt { get; set; }

        [Display(Name = "تاریخ به‌روزرسانی")]
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        [Display(Name = "ایجاد شده توسط")]
        public int? CreatedById { get; set; }

        [Display(Name = "به‌روزرسانی شده توسط")]
        public int? UpdatedById { get; set; }

        [Display(Name = "پروژه")]
        public int? ProjectId { get; set; }

        // Navigation Properties
        [ForeignKey("CreatedById")]
        public virtual ApplicationUser? CreatedBy { get; set; }

        [ForeignKey("UpdatedById")]
        public virtual ApplicationUser? UpdatedBy { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        public virtual ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
        public virtual ICollection<InvoiceEmailLog> EmailLogs { get; set; } = new List<InvoiceEmailLog>();
        public virtual ICollection<InvoiceTelegramLog> TelegramLogs { get; set; } = new List<InvoiceTelegramLog>();
    }

    public enum InvoiceStatus
    {
        [Display(Name = "پیش‌نویس")]
        Draft = 1,
        [Display(Name = "ارسال شده")]
        Sent = 2,
        [Display(Name = "پرداخت شده")]
        Paid = 3,
        [Display(Name = "لغو شده")]
        Cancelled = 4
    }
}
