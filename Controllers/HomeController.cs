using System;
using System.Diagnostics;
using System.Linq;
using HP_Detailing.Models;
using HP_Detailing.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HP_Detailing.Controllers
{
    // ========================================================
    // CONTROLLER: HomeController
    // CHỨC NĂNG: Quản trị Trang chủ (Dashboard) hiển thị số liệu thống kê live của hệ thống
    // BẢO MẬT: Dùng thuộc tính [Authorize] bắt buộc phải đăng nhập thành công mới được truy cập
    // ========================================================
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HP_Detailing.Data.HP_DetailingDbContext _context;

        public HomeController(ILogger<HomeController> logger, HP_Detailing.Data.HP_DetailingDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // ========================================================
        // ACTION: Index (Trang chủ Dashboard)
        // NGHIỆP VỤ: Đọc doanh thu, số xe hoàn thành, xe đang thi công, lịch hẹn ngày hôm nay từ SQL Server
        // BẮT LỖI: Try-catch bao bọc chặt chẽ để trả về trang lỗi thân thiện nếu mất kết nối CSDL
        // ========================================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string? date = null, string shift = "all")
        {
            // Nếu chưa đăng nhập, trả về trang chủ công khai (landing page)
            if (!User?.Identity?.IsAuthenticated ?? true)
            {
                // Load active services (with category) for booking dropdown & pricing table
                var services = _context.Services
                    .Include(s => s.ServiceCategory)
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.ServiceCategoryId)
                    .ThenBy(s => s.UnitPrice)
                    .ToList();
                ViewBag.Services = services;
                ViewData["Title"] = "HP Auto - Trang chủ";
                return View("PublicIndex");
            }

            try
            {
                // ── Parse ngày lọc (mặc định = hôm nay theo local time VN) ──
                var localOffset = TimeSpan.FromHours(7); // UTC+7
                var filterDate = DateTime.TryParse(date, out var parsedDate)
                    ? parsedDate.Date
                    : DateTime.UtcNow.Add(localOffset).Date;

                // ── Tính mốc thời gian theo ca trực (lưu dưới dạng UTC để so sánh DB) ──
                DateTime filterFrom, filterTo;
                switch (shift)
                {
                    case "morning":
                        filterFrom = filterDate.AddHours(7).Add(-localOffset);   // 07:00 VN
                        filterTo   = filterDate.AddHours(12).Add(-localOffset);  // 12:00 VN
                        break;
                    case "afternoon":
                        filterFrom = filterDate.AddHours(12).Add(-localOffset);
                        filterTo   = filterDate.AddHours(18).Add(-localOffset);
                        break;
                    case "evening":
                        filterFrom = filterDate.AddHours(18).Add(-localOffset);
                        filterTo   = filterDate.AddHours(23).Add(-localOffset);
                        break;
                    default: // "all" – cả ngày
                        filterFrom = filterDate.Add(-localOffset);
                        filterTo   = filterDate.AddDays(1).Add(-localOffset);
                        break;
                }

                var dashboardData = new HomeDashboardViewModel
                {
                    FilterDate = filterDate,
                    FilterShift = shift,
                    FilterFrom  = filterFrom,
                    FilterTo    = filterTo,

                    // 1. Doanh thu: chỉ lấy hóa đơn PAID có PaidAt trong khoảng lọc
                    TotalRevenue = _context.Invoices
                        .Where(i => i.Status == "PAID"
                                    && i.PaidAt.HasValue
                                    && i.PaidAt.Value >= filterFrom
                                    && i.PaidAt.Value < filterTo)
                        .Sum(i => (decimal?)i.TotalAmount) ?? 0m,

                    // 2. Phiếu hoàn thành trong khoảng lọc
                    CompletedTickets = _context.Tickets
                        .Count(t => t.Status == "completed"
                                    && t.CreatedAt >= filterFrom
                                    && t.CreatedAt < filterTo),

                    // 3. Xe đang thi công: real-time, không lọc theo thời gian
                    InProgressCars = _context.Tickets
                        .Count(t => t.Status == "in_progress"),

                    // 4. Lịch hẹn trong khoảng lọc
                    TodayAppointmentsCount = _context.Appointments
                        .Count(a => a.AppointmentTime >= filterFrom && a.AppointmentTime < filterTo),

                    TodayAppointmentsList = _context.Appointments
                        .Where(a => a.AppointmentTime >= filterFrom && a.AppointmentTime < filterTo)
                        .OrderBy(a => a.AppointmentTime)
                        .ToList(),

                    // 5. Phiếu dịch vụ gần đây trong khoảng ngày đang chọn
                    RecentTickets = _context.Tickets
                        .Where(t => t.CreatedAt >= filterFrom && t.CreatedAt < filterTo)
                        .OrderByDescending(t => t.CreatedAt)
                        .Take(10)
                        .ToList()
                };

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI TRUY VẤN DỮ LIỆU DASHBOARD HỆ THỐNG");
                return RedirectToAction("Error");
            }
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Terms()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult WarrantyPolicy()
        {
            return View();
        }

        // ========================================================
        // ACTION: Error (Trang hiển thị lỗi thân thiện)
        // ========================================================
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
