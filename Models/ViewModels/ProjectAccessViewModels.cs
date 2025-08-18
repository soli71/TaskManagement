using System.ComponentModel.DataAnnotations;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Models.ViewModels
{
    public class ProjectAccessViewModel
    {
        public int Id { get; set; }
        
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        
        [Display(Name = "کاربر")]
        [Required(ErrorMessage = "انتخاب کاربر الزامی است")]
        public int UserId { get; set; }
        
        [Display(Name = "یادداشت")]
        [StringLength(500, ErrorMessage = "یادداشت نمی‌تواند بیش از 500 کاراکتر باشد")]
        public string? Notes { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime GrantedAt { get; set; }
        public string? GrantedByName { get; set; }
        public string? UserFullName { get; set; }
        public string? UserCompanyName { get; set; }
        public List<string> UserRoles { get; set; } = new();
    }

    public class ProjectAccessListViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public List<ProjectAccessViewModel> AccessList { get; set; } = new();
        public List<ApplicationUser> AvailableUsers { get; set; } = new();
    }

    public class ManageProjectAccessViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        
        [Display(Name = "کاربران")]
        [Required(ErrorMessage = "انتخاب حداقل یک کاربر الزامی است")]
        public List<int> SelectedUserIds { get; set; } = new();
        
        [Display(Name = "یادداشت")]
        [StringLength(500, ErrorMessage = "یادداشت نمی‌تواند بیش از 500 کاراکتر باشد")]
        public string? Notes { get; set; }
        
        public List<ApplicationUser> AvailableUsers { get; set; } = new();
        public List<ProjectAccessViewModel> CurrentAccess { get; set; } = new();
    }
}
