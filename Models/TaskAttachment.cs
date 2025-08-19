using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementMvc.Models
{
    public class TaskAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(100)]
        public string ContentType { get; set; } = "application/octet-stream";

        public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; }

        public int? UploadedById { get; set; }

        [ForeignKey("UploadedById")]
        public virtual ApplicationUser? UploadedBy { get; set; }

        [Required]
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual TaskItem Task { get; set; } = null!;
    }
}


