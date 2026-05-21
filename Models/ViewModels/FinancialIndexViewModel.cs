using System.Collections.Generic;

namespace HP_Detailing.Models.ViewModels
{
    public class FinancialIndexViewModel
    {
        public decimal MonthlyRevenue { get; set; }
        public int UnpaidCount { get; set; }
        public decimal TotalUnpaidAmount { get; set; }
        public int PaidCount { get; set; }
        public List<InvoiceItemViewModel> Invoices { get; set; } = new();
    }

    public class InvoiceItemViewModel
    {
        public int Id { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public string TicketCode { get; set; } = string.Empty;
        public int TicketId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string VehiclePlate { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "UNPAID"; // PAID / UNPAID
        public string CreatedDateFormatted { get; set; } = string.Empty;
    }
}
