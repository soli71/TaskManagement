using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskManagementMvc.Models.ViewModels
{
    public class GradeFormViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BaseHourlyRate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
