using System.Collections.Generic;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Models.ViewModels
{
    public class CompanyOverviewViewModel
    {
        public int? SelectedCompanyId { get; set; }
        public List<Company> Companies { get; set; } = new List<Company>();
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}


