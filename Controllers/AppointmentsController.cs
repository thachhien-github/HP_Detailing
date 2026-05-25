using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using HP_Detailing.Models;
using HP_Detailing.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using HP_Detailing.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using HP_Detailing.Hubs;

namespace HP_Detailing.Controllers
{
    // ========================================================
    // CONTROLLER: AppointmentsController
    // CHỨC NĂNG: Quản trị Lịch đặt hẹn của khách hàng, tích hợp AJAX SPA ngầm
    // BẢO MẬT: Bắt buộc đăng nhập hệ thống mới được quản lý
    // ========================================================
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly HP_Detailing.Data.HP_DetailingDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AppointmentsController(HP_Detailing.Data.HP_DetailingDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // ========================================================
        // ACTION: Index (Danh sách Lịch hẹn)
        // ========================================================
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                var vm = new AppointmentListViewModel
                {
                    Appointments = _context.Appointments
                        .OrderByDescending(a => a.AppointmentTime)
                        .Take(10)
                        .ToList()
                };
                return View(vm);
            }
            catch (Exception)
            {
                // Trả về view trống hoặc trang báo lỗi nếu gặp lỗi CSDL
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> List(int page = 1, int pageSize = 10, string? search = null, string? status = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Appointments
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var keyword = search.Trim();
                    query = query.Where(a =>
                        (a.CustomerName != null && a.CustomerName.Contains(keyword)) ||
                        (a.CustomerPhone != null && a.CustomerPhone.Contains(keyword)) ||
                        (a.Plate != null && a.Plate.Contains(keyword)));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(a => a.Status == status);
                }

                query = query.OrderByDescending(a => a.AppointmentTime);

                var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
                var items = paged.Items.Select(a => new AppointmentListItemDto
                {
                    Id = a.Id,
                    AppointmentCode = a.AppointmentCode,
                    CustomerName = a.CustomerName ?? string.Empty,
                    CustomerPhone = a.CustomerPhone,
                    Plate = a.Plate,
                    Services = a.Services,
                    Note = a.Note,
                    AppointmentTime = a.AppointmentTime,
                    Status = a.Status
                }).ToList();

                return Json(new PagedResult<AppointmentListItemDto>
                {
                    Items = items,
                    TotalCount = paged.TotalCount,
                    Page = paged.Page,
                    PageSize = paged.PageSize,
                    TotalPages = paged.TotalPages
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Không thể tải danh sách lịch hẹn: " + ex.Message });
            }
        }

        // ========================================================
        // AJAX API: Create (Tạo lịch hẹn mới qua AJAX)
        // Cho phép khách chưa đăng nhập đặt lịch qua trang công khai
        // ========================================================
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] AppointmentRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.CustomerName) || string.IsNullOrEmpty(request.CustomerPhone))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ Tên khách hàng và Số điện thoại!" });
                }

                // Phát sinh mã lịch hẹn thông minh dạng LH2405-001
                var dateStr = DateTime.UtcNow.ToString("yyMMdd");
                var countToday = _context.Appointments.Count(a => a.AppointmentCode.Contains("LH" + dateStr)) + 1;
                var appointmentCode = $"LH{dateStr}-{countToday:D3}";

                var newApt = new Appointment
                {
                    AppointmentCode = appointmentCode,
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone,
                    Plate = request.Plate,
                    AppointmentTime = request.AppointmentTime == default ? DateTime.UtcNow.AddHours(2) : request.AppointmentTime,
                    Services = request.Services,
                    Note = request.Note,
                    Status = "pending"
                };

                _context.Appointments.Add(newApt);
                await _context.SaveChangesAsync();

                // Gửi thông báo realtime khi có lịch hẹn mới
                await NotificationHelper.SendNotificationAsync(
                    _context,
                    _hubContext,
                    "Lịch hẹn mới",
                    $"Khách hàng {newApt.CustomerName} ({newApt.Plate ?? "Không có biển số"}) vừa đặt lịch hẹn lúc {newApt.AppointmentTime:dd/MM/yyyy HH:mm}.",
                    "Appointment",
                    "/appointments"
                );

                return Json(new { success = true, message = "Lưu lịch đặt hẹn thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống khi lưu lịch hẹn: " + ex.Message });
            }
        }

        // ========================================================
        // AJAX API: UpdateStatus (Cập nhật trạng thái lịch hẹn)
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateAptStatusRequest request)
        {
            try
            {
                if (request == null || request.AppointmentId <= 0 || string.IsNullOrEmpty(request.Status))
                {
                    return Json(new { success = false, message = "Dữ liệu trạng thái truyền lên không hợp lệ!" });
                }

                var apt = _context.Appointments.Find(request.AppointmentId);
                if (apt == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin lịch hẹn yêu cầu!" });
                }

                apt.Status = request.Status;
                await _context.SaveChangesAsync();

                string statusText = request.Status == "arrived" ? "đã tiếp nhận xe" : (request.Status == "cancelled" ? "đã hủy" : request.Status);
                await NotificationHelper.SendNotificationAsync(
                    _context,
                    _hubContext,
                    "Cập nhật lịch hẹn",
                    $"Lịch hẹn {apt.AppointmentCode} của khách hàng {apt.CustomerName} đã chuyển sang trạng thái: {statusText}.",
                    "Appointment",
                    "/appointments"
                );

                return Json(new { success = true, message = "Cập nhật trạng thái lịch hẹn thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }

    // ========================================================
    // REQUEST DTOS HỖ TRỢ TRUYỀN DỮ LIỆU AJAX
    // ========================================================
    public class AppointmentRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? Plate { get; set; }
        public DateTime AppointmentTime { get; set; }
        public string? Services { get; set; }
        public string? Note { get; set; }
    }

    public class UpdateAptStatusRequest
    {
        public int AppointmentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AppointmentListItemDto
    {
        public int Id { get; set; }
        public string AppointmentCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? Plate { get; set; }
        public DateTime AppointmentTime { get; set; }
        public string? Services { get; set; }
        public string? Note { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
