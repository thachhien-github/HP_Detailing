using HP_Detailing.Models;
using Microsoft.EntityFrameworkCore;

namespace HP_Detailing.Data
{
    /// <summary>
    /// Đồng bộ hóa đơn (tổng tiền + dòng InvoiceService) từ phiếu dịch vụ.
    /// Chỉ cập nhật hóa đơn UNPAID; PAID/CANCELLED giữ nguyên.
    /// </summary>
    public static class InvoiceSync
    {
        public static decimal SyncFromTicket(HP_DetailingDbContext context, int ticketId)
        {
            var ticket = context.Tickets
                .Include(t => t.TicketServices)
                    .ThenInclude(ts => ts.Service)
                .Include(t => t.TicketMaterialUsages)
                    .ThenInclude(tmu => tmu.Material)
                .FirstOrDefault(t => t.Id == ticketId);

            if (ticket == null)
                return 0;

            var invoice = context.Invoices.FirstOrDefault(i => i.TicketId == ticketId);
            if (invoice == null)
            {
                invoice = new Invoice
                {
                    InvoiceCode = $"HD-{ticket.TicketCode}",
                    TicketId = ticketId,
                    Status = "UNPAID",
                    CreatedAt = DateTime.UtcNow,
                    TotalAmount = 0
                };
                context.Invoices.Add(invoice);
                context.SaveChanges();
            }

            if (invoice.Status == "PAID" || invoice.Status == "CANCELLED")
                return invoice.TotalAmount;

            var existingLines = context.InvoiceServices.Where(s => s.InvoiceId == invoice.Id);
            context.InvoiceServices.RemoveRange(existingLines);

            var lines = BuildLines(invoice.Id, ticket);
            if (lines.Count > 0)
                context.InvoiceServices.AddRange(lines);

            invoice.TotalAmount = lines.Sum(l => l.Price);
            context.SaveChanges();
            return invoice.TotalAmount;
        }

        private static List<InvoiceService> BuildLines(int invoiceId, Ticket ticket)
        {
            var lines = new List<InvoiceService>();

            foreach (var ts in ticket.TicketServices)
            {
                lines.Add(new InvoiceService
                {
                    InvoiceId = invoiceId,
                    Name = ts.Service?.Name ?? "Dịch vụ",
                    Price = ts.PriceSnapshot
                });
            }

            foreach (var tm in ticket.TicketMaterialUsages.Where(m => m.IsChargedToCustomer))
            {
                var matName = tm.Material?.Name ?? "Vật tư";
                var unit = tm.Material?.Unit;
                var qtyLabel = !string.IsNullOrEmpty(unit)
                    ? $"{tm.Quantity} {unit}"
                    : tm.Quantity.ToString("0.##");
                lines.Add(new InvoiceService
                {
                    InvoiceId = invoiceId,
                    Name = $"{matName} ({qtyLabel})",
                    Price = tm.Quantity * tm.UnitPrice
                });
            }

            return lines;
        }

        /// <summary>Đồng bộ lại tất cả hóa đơn UNPAID (dùng khi khởi động / backfill).</summary>
        public static void ResyncAllUnpaid(HP_DetailingDbContext context)
        {
            var ticketIds = context.Invoices
                .Where(i => i.Status == "UNPAID" && i.TicketId != null)
                .Select(i => i.TicketId!.Value)
                .Distinct()
                .ToList();

            foreach (var ticketId in ticketIds)
                SyncFromTicket(context, ticketId);
        }
    }
}
