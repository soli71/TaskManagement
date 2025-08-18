using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagementMvc.Models.ViewModels
{
    public class ProjectFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام پروژه الزامی است")]
        [Display(Name = "نام پروژه")]
        [StringLength(200, ErrorMessage = "نام پروژه نمی‌تواند بیشتر از 200 کاراکتر باشد")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "توضیحات")]
        [StringLength(1000, ErrorMessage = "توضیحات نمی‌تواند بیشتر از 1000 کاراکتر باشد")]
        public string? Description { get; set; }

        [Display(Name = "کد پروژه")]
        [StringLength(50, ErrorMessage = "کد پروژه نمی‌تواند بیشتر از 50 کاراکتر باشد")]
        public string? Code { get; set; }

        [Display(Name = "تاریخ شروع")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "تاریخ پایان پیش‌بینی شده")]
        [DataType(DataType.Date)]
        public DateTime? EstimatedEndDate { get; set; }

        [Display(Name = "تاریخ پایان واقعی")]
        [DataType(DataType.Date)]
        public DateTime? ActualEndDate { get; set; }

        [Display(Name = "بودجه")]
        [Range(0, double.MaxValue, ErrorMessage = "بودجه باید عدد مثبت باشد")]
        public decimal? Budget { get; set; }

        [Display(Name = "هزینه واقعی")]
        [Range(0, double.MaxValue, ErrorMessage = "هزینه واقعی باید عدد مثبت باشد")]
        public decimal? ActualCost { get; set; }

        [Display(Name = "اولویت")]
        public TaskManagementMvc.Models.ProjectPriority Priority { get; set; } = TaskManagementMvc.Models.ProjectPriority.Medium;

        [Display(Name = "وضعیت")]
        public TaskManagementMvc.Models.ProjectStatus? Status { get; set; }

        [Display(Name = "مدیر پروژه")]
        public int? ProjectManagerId { get; set; }

        [Required(ErrorMessage = "انتخاب شرکت الزامی است")]
        [Display(Name = "شرکت")]
        public int CompanyId { get; set; }

        public List<Company> Companies { get; set; } = new List<Company>();
        public List<ApplicationUser> ProjectManagers { get; set; } = new List<ApplicationUser>();
    }
}