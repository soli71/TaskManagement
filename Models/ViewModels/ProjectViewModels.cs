using System;
using System.Collections.Generic;

namespace TaskManagementMvc.Models.ViewModels
{
    public class ProjectFormViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Code { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EstimatedEndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }

        public decimal? Budget { get; set; }
        public decimal? ActualCost { get; set; }

        public TaskManagementMvc.Models.ProjectPriority Priority { get; set; } = TaskManagementMvc.Models.ProjectPriority.Medium;
        public TaskManagementMvc.Models.ProjectStatus Status { get; set; } = TaskManagementMvc.Models.ProjectStatus.Active;

        public int CompanyId { get; set; }
        public int? ProjectManagerId { get; set; }

        public List<Company> Companies { get; set; } = new List<Company>();
        public List<ApplicationUser> ProjectManagers { get; set; } = new List<ApplicationUser>();
    }
}


