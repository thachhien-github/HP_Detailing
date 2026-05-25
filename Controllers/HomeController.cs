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
        public IActionResult Index()
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
                // Lấy ngày hôm nay theo giờ hệ thống local
                var today = DateTime.UtcNow.Date;

                // Khởi tạo ViewModel chứa dữ liệu live từ cơ sở dữ liệu
                var dashboardData = new HomeDashboardViewModel();

                // 1. Tính tổng doanh thu thực tế từ các hoá đơn đã thanh toán (PAID)
                dashboardData.TotalRevenue = _context.Invoices
                    .Where(i => i.Status == "PAID")
                    .Sum(i => i.TotalAmount);

                // 2. Đếm số lượng phiếu tiếp nhận đã thi công hoàn thành
                dashboardData.CompletedTickets = _context.Tickets
                    .Count(t => t.Status == "completed");

                // 3. Đếm số lượng xe đang trong xưởng thi công (in_progress)
                dashboardData.InProgressCars = _context.Tickets
                    .Count(t => t.Status == "in_progress");

                // 4. Lấy danh sách lịch đặt hẹn hôm nay (lọc theo ngày)
                var todayAppointments = _context.Appointments
                    .Where(a => a.AppointmentTime.Date == today)
                    .OrderBy(a => a.AppointmentTime)
                    .ToList();

                dashboardData.TodayAppointmentsCount = todayAppointments.Count;
                dashboardData.TodayAppointmentsList = todayAppointments;

                // 5. Lấy danh sách 5 Phiếu tiếp nhận dịch vụ mới nhất hiển thị lên bảng điều khiển
                dashboardData.RecentTickets = _context.Tickets
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .ToList();

                // Trả về view kèm theo dữ liệu live đã nạp đầy đủ
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                // Ghi nhận lỗi hệ thống chi tiết vào log phục vụ chẩn đoán
                _logger.LogError(ex, "LỖI TRUY VẤN DỮ LIỆU DASHBOARD HỆ THỐNG");

                // Trả về trang lỗi mặc định thay vì làm treo ứng dụng đột ngột
                return RedirectToAction("Error");
            }
        }

        public IActionResult Privacy()
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
