using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskManagementMvc.Models.ViewModels
{
    public class InvoiceFormViewModel
    {
        public int Id { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }

        public int CompanyId { get; set; }
        public int ProjectId { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string? Notes { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        public List<SelectListItem> Companies { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
        public List<InvoiceLineViewModel> Lines { get; set; } = new List<InvoiceLineViewModel>();
    }

    public class InvoiceLineViewModel
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int? TaskId { get; set; }
        public string? TaskTitle { get; set; }
    }

    // View model dedicated for Create action binding (only required fields)
    public class InvoiceCreateViewModel
    {
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public string? Description { get; set; }
        public string? CustomerName { get; set; }
        public List<TaskSelectionViewModel> Tasks { get; set; } = new();
    }

    public class TaskSelectionViewModel
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double HoursAvailable { get; set; }
        public bool Selected { get; set; }
        public double HoursForInvoice { get; set; }
        public string? PerformerName { get; set; }
        public DateTime? StartAt { get; set; }
    }
}

