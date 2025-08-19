using System.ComponentModel.DataAnnotations;

namespace TaskManagementMvc.Models
{
    public class ProjectAccess
    {
        public int Id { get; set; }
        
        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        
        [Required]
        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
        
    public DateTime GrantedAt { get; set; }
        
        public int? GrantedById { get; set; }
        public ApplicationUser? GrantedBy { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public string? Notes { get; set; }
        
        // فیلدهای جدید برای مدیریت کامل دسترسی
        public string? Reason { get; set; }
        
        public DateTime? RevokedAt { get; set; }
        
        public int? RevokedById { get; set; }
        public ApplicationUser? RevokedBy { get; set; }
        
        public string? RevokeReason { get; set; }
    }
}
