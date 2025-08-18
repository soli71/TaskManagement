using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskManagementMvc.Models.ViewModels
{
    public class CreateRoleViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> SelectedPermissions { get; set; } = new List<string>();
        public List<Permission> Permissions { get; set; } = new List<Permission>();
    }

    public class EditRoleViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string> SelectedPermissions { get; set; } = new List<string>();
        public List<RolePermissionViewModel> CurrentPermissions { get; set; } = new List<RolePermissionViewModel>();
        public List<Permission> Permissions { get; set; } = new List<Permission>();
    }

    public class RolePermissionViewModel
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public string? AssignedBy { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoleListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class PermissionGroupViewModel
    {
        public string GroupName { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public List<PermissionViewModel> Permissions { get; set; } = new List<PermissionViewModel>();
    }

    public class PermissionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RoleFormViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<SelectListItem> Permissions { get; set; } = new List<SelectListItem>();
    }
}
