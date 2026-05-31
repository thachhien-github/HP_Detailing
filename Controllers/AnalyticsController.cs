using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HP_Detailing.Data;
using HP_Detailing.Models;

namespace HP_Detailing.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : Controller
    {
        private readonly HP_DetailingDbContext _context;
        public AnalyticsController(HP_DetailingDbContext context) { _context = context; }

        public IActionResult Index(int? year, int? month, DateTime? fromDate, DateTime? toDate)
        {
            var now = DateTime.Now;
            
            // Determine filter bounds
            DateTime startDate;
            DateTime endDate;

            if (fromDate.HasValue && toDate.HasValue)
            {
                startDate = fromDate.Value.Date;
                endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
            }
            else if (year.HasValue && month.HasValue)
            {
                startDate = new DateTime(year.Value, month.Value, 1);
                endDate = startDate.AddMonths(1).AddTicks(-1);
            }
            else if (year.HasValue)
            {
                startDate = new DateTime(year.Value, 1, 1);
                endDate = startDate.AddYears(1).AddTicks(-1);
            }
            else
            {
                startDate = new DateTime(now.Year, now.Month, 1);
                endDate = now;
            }

            var previousStartDate = startDate.AddDays(-(endDate - startDate).TotalDays);
            var previousEndDate = startDate.AddTicks(-1);

            // ── Invoices ──────────────────────────────────────────────
            var paidInvoices = _context.Invoices
                .Where(i => i.Status != null && i.Status.ToUpper() == "PAID")
                .ToList();

            var thisPeriodRevenue = paidInvoices
                .Where(i => (i.PaidAt ?? i.CreatedAt) >= startDate && (i.PaidAt ?? i.CreatedAt) <= endDate)
                .Sum(i => i.TotalAmount);

            var lastPeriodRevenue = paidInvoices
                .Where(i => (i.PaidAt ?? i.CreatedAt) >= previousStartDate && (i.PaidAt ?? i.CreatedAt) <= previousEndDate)
                .Sum(i => i.TotalAmount);

            // ── Tickets ───────────────────────────────────────────────
            var completedTickets = _context.Tickets.Count(t => (t.Status == "completed" || t.Status == "done") && t.CreatedAt >= startDate && t.CreatedAt <= endDate);
            var thisMonthTickets = _context.Tickets.Count(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate);

            // ── Revenue last 12 months (bar chart) ───────────────────
            var barChartEndMonth = new DateTime(endDate.Year, endDate.Month, 1);
            var revenueByMonth = new List<(int Month, int Year, decimal Revenue)>();
            for (int i = 11; i >= 0; i--)
            {
                var m = barChartEndMonth.AddMonths(-i);
                var rev = paidInvoices
                    .Where(inv => (inv.PaidAt ?? inv.CreatedAt).Year == m.Year && (inv.PaidAt ?? inv.CreatedAt).Month == m.Month)
                    .Sum(inv => inv.TotalAmount);
                revenueByMonth.Add((m.Month, m.Year, rev));
            }

            // ── Top Services ──────────────────────────────────────────
            var topServices = _context.TicketServices
                .Include(ts => ts.Service)
                .GroupBy(ts => ts.Service!.Name)
                .Select(g => new {
                    Name = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(x => x.PriceSnapshot)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            // ── Service Category breakdown for donut ─────────────────
            var categoryRevenue = _context.TicketServices
                .Include(ts => ts.Service).ThenInclude(s => s!.ServiceCategory)
                .GroupBy(ts => ts.Service!.ServiceCategory!.Name)
                .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.PriceSnapshot) })
                .OrderByDescending(x => x.Revenue)
                .Take(4)
                .ToList();

            decimal totalCatRevenue = categoryRevenue.Sum(c => c.Revenue);

            // ── Top Staff by service count ────────────────────────────
            var topStaff = _context.Staff
                .Include(s => s.LaborContracts)
                .Where(s => s.IsActive)
                .OrderBy(s => s.StaffCode)
                .Take(4)
                .ToList();

            // ── Stock cost ────────────────────────────────────────────
            var stockCost = _context.WarehouseStocks
                .Include(w => w.Material)
                .Sum(w => w.QuantityOnHand * (w.Material != null ? w.Material.UnitPrice : 0));

            // ── Pass to ViewBag ───────────────────────────────────────
            ViewBag.ThisPeriodRevenue = thisPeriodRevenue;
            ViewBag.LastPeriodRevenue = lastPeriodRevenue;
            ViewBag.RevenueGrowth = lastPeriodRevenue > 0
                ? Math.Round((thisPeriodRevenue - lastPeriodRevenue) / lastPeriodRevenue * 100, 1)
                : 0m;
            ViewBag.CompletedTickets = completedTickets;
            ViewBag.ThisMonthTickets = thisMonthTickets;
            ViewBag.StockCost = stockCost;
            ViewBag.RevenueByMonth = revenueByMonth;
            ViewBag.TopServices = topServices;
            ViewBag.CategoryRevenue = categoryRevenue;
            ViewBag.TotalCatRevenue = totalCatRevenue;
            ViewBag.TopStaff = topStaff;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View();
        }
    }
}
