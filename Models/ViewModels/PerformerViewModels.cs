using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskManagementMvc.Models.ViewModels
{
    public class PerformerFormViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }

        public int CompanyId { get; set; }
        public int GradeId { get; set; }

        public decimal HourlyRate { get; set; }
        public bool IsActive { get; set; } = true;

        public List<SelectListItem> Companies { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Grades { get; set; } = new List<SelectListItem>();
    }
}
