using System.ComponentModel.DataAnnotations;

namespace TaskManagementMvc.Models
{
    public class EmailTemplate
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "نام قالب الزامی است")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "موضوع ایمیل الزامی است")]
        public string Subject { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "متن قالب الزامی است")]
        public string Body { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public string? UpdatedBy { get; set; }
        
        // Template variables that can be replaced
        public string? AvailableVariables { get; set; }
    }
}
