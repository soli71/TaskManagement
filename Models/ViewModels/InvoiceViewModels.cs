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
}
