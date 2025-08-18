using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementMvc.Models
{
    /// <summary>
    /// لاگ اجراهای زمان‌بندی صدور خودکار فاکتور.
    /// </summary>
    public class InvoiceJobRunLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ScheduleId { get; set; }

        [ForeignKey(nameof(ScheduleId))]
        public virtual InvoiceSchedule Schedule { get; set; } = null!;

        public DateTime RunStartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RunCompletedAt { get; set; }

        public int? InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public virtual Invoice? Invoice { get; set; }

        public int TasksCount { get; set; }
        public bool IsSuccess { get; set; }

        [MaxLength(1000)]
        public string? Error { get; set; }
    }
}
