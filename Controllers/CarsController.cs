using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HP_Detailing.Models;
using HP_Detailing.Models.ViewModels;

namespace HP_Detailing.Controllers
{
    [Authorize]
    public class CarsController : Controller
    {
        private readonly HP_Detailing.Data.HP_DetailingDbContext _context;

        public CarsController(HP_Detailing.Data.HP_DetailingDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cars = _context.Cars.AsNoTracking().ToList();

            var ticketsForStats = _context.Tickets
                .AsNoTracking()
                .Where(t => t.Plate != null && t.Plate != "")
                .ToList();

            var ticketStats = ticketsForStats
                .GroupBy(t => t.Plate!.Trim().ToUpper(), StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var latest = g.OrderByDescending(t => t.CreatedAt).First();
                        return new { LastVisit = latest.CreatedAt, LatestStatus = latest.Status ?? "—" };
                    },
                    StringComparer.Ordinal);

            var latestModelsByPlate = ticketsForStats
                .GroupBy(t => t.Plate!.Trim().ToUpper(), StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(t => t.CreatedAt).Select(t => t.CarModel).FirstOrDefault(),
                    StringComparer.Ordinal);

            var items = cars.Select(c =>
            {
                ticketStats.TryGetValue(c.Plate, out var stats);
                latestModelsByPlate.TryGetValue(c.Plate, out var latestTicketModel);

                return new CarIndexItem
                {
                    Plate = c.Plate,
                    Brand = c.Brand,
                    Model = c.Model,
                    Color = c.Color,
                    CarModel = FormatCarDisplay(c.Brand, c.Model, latestTicketModel),
                    CustomerName = c.OwnerName ?? "Chưa cập nhật",
                    CustomerPhone = c.OwnerPhone ?? "",
                    Status = stats?.LatestStatus ?? "—",
                    LastVisit = stats?.LastVisit ?? c.CreatedAt
                };
            })
            .OrderByDescending(c => c.LastVisit)
            .ToList();

            return View(new CarIndexViewModel { Cars = items });
        }

        [HttpGet]
        public IActionResult GetCarDetails(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return Json(new { success = false, message = "Biển số không hợp lệ!" });

            var plateNorm = plate.Trim().ToUpper();
            var car = _context.Cars.AsNoTracking().FirstOrDefault(c => c.Plate == plateNorm);

            var tickets = _context.Tickets
                .Include(t => t.TicketServices)
                    .ThenInclude(ts => ts.Service)
                .Where(t => t.Plate == plateNorm)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            if (car == null && !tickets.Any())
                return Json(new { success = false, message = "Không tìm thấy hồ sơ xe!" });

            var latestTicket = tickets.FirstOrDefault();
            var ticketIds = tickets.Select(t => t.Id).ToList();
            var invoiceByTicket = _context.Invoices
                .AsNoTracking()
                .Where(i => i.TicketId != null && ticketIds.Contains(i.TicketId.Value))
                .ToDictionary(i => i.TicketId!.Value);

            var dto = new CarDetailDto
            {
                Plate = plateNorm,
                Brand = car?.Brand,
                Model = car?.Model,
                Color = car?.Color,
                CarModel = FormatCarDisplay(car?.Brand, car?.Model, latestTicket?.CarModel),
                CustomerName = car?.OwnerName ?? latestTicket?.CustomerName ?? "Khách vãng lai",
                CustomerPhone = car?.OwnerPhone ?? latestTicket?.CustomerPhone ?? "",
                History = tickets.Select(t =>
                {
                    invoiceByTicket.TryGetValue(t.Id, out var invoice);
                    var totalAmount = invoice != null
                        ? invoice.TotalAmount
                        : t.TicketServices.Sum(ts => ts.PriceSnapshot);
                    var status = invoice != null && invoice.Status == "PAID" ? "Đã thanh toán" : t.Status;

                    var servicesSummary = t.TicketServices.Any()
                        ? string.Join(", ", t.TicketServices.Select(ts => ts.Service?.Name ?? "Dịch vụ"))
                        : "Không có dịch vụ";

                    return new CarHistoryItemDto
                    {
                        TicketId = t.Id,
                        TicketCode = t.TicketCode,
                        CreatedAtStr = t.CreatedAt.ToString("dd/MM/yyyy"),
                        ServicesSummary = servicesSummary,
                        TotalAmountStr = totalAmount.ToString("N0") + "đ",
                        Status = status
                    };
                }).ToList()
            };

            return Json(new { success = true, data = dto });
        }

        private static string FormatCarDisplay(string? brand, string? model, string? fallback)
        {
            var parts = new[] { brand, model }.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            if (parts.Count > 0)
                return string.Join(" ", parts);
            return string.IsNullOrWhiteSpace(fallback) ? "Chưa cập nhật" : fallback!;
        }
    }
}
