using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskManagementMvc.Models.ViewModels
{
    public class CreateUserViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Notes { get; set; }
        public int? CompanyId { get; set; }
        public int? GradeId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? IbanNumber { get; set; }
        public string? CardNumber { get; set; }
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public List<string> SelectedRoles { get; set; } = new List<string>();
        
        // Lists for dropdowns
        public List<Company> Companies { get; set; } = new List<Company>();
        public List<Grade> Grades { get; set; } = new List<Grade>();
        public List<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();
    }

    public class EditUserViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Notes { get; set; }
        public int? CompanyId { get; set; }
        public int? GradeId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? IbanNumber { get; set; }
        public string? CardNumber { get; set; }
        public List<string> SelectedRoles { get; set; } = new List<string>();
        public List<UserRoleViewModel> CurrentRoles { get; set; } = new List<UserRoleViewModel>();
        
        // Lists for dropdowns
        public List<Company> Companies { get; set; } = new List<Company>();
        public List<Grade> Grades { get; set; } = new List<Grade>();
        public List<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();
    }

    public class UserRoleViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public string? AssignedBy { get; set; }
        public bool IsActive { get; set; }
    }

    public class ChangePasswordViewModel
    {
        public int UserId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UserListViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? CompanyName { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class UserFormViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int? CompanyId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<SelectListItem> Companies { get; set; } = new List<SelectListItem>();
    }
}
