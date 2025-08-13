using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskManagementMvc.Models.ViewModels
{
    public class CompanyFormViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CompanyDetailsViewModel
    {
        public Company Company { get; set; } = null!;
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
