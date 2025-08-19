using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementMvc.Models
{
    public class InvoiceEmailLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string ToEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }

        [StringLength(1000)]
        public string? Error { get; set; }

    public DateTime SentAt { get; set; }

        public int? SentById { get; set; }

        [ForeignKey("SentById")]
        public virtual ApplicationUser? SentBy { get; set; }
    }
}
