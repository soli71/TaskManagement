using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementMvc.Models
{
    public class TaskHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual TaskItem Task { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Field { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? OldValue { get; set; }

        [StringLength(1000)]
        public string? NewValue { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

    public DateTime ChangedAt { get; set; }

        public int? ChangedById { get; set; }

        [ForeignKey("ChangedById")]
        public virtual ApplicationUser? ChangedBy { get; set; }
    }
}


