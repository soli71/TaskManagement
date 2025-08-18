using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Models.ViewModels
{
    public class TaskFormViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskManagementMvc.Models.TaskStatus Status { get; set; } = TaskManagementMvc.Models.TaskStatus.InProgress;
        public TaskManagementMvc.Models.TaskPriority Priority { get; set; } = TaskManagementMvc.Models.TaskPriority.Medium;

        public double Hours { get; set; }
        public double? OriginalEstimateHours { get; set; }

        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        public int? PerformerId { get; set; }
        public int? ProjectId { get; set; }
		public int? CompanyId { get; set; }

        public List<SelectListItem> Performers { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
		public List<SelectListItem> Companies { get; set; } = new List<SelectListItem>();
        public List<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
        public List<TaskHistory> HistoryEntries { get; set; } = new List<TaskHistory>();
    }
}