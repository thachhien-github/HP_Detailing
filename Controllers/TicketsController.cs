using System;
using System.Linq;
using System.Collections.Generic;
using HP_Detailing.Models;
using HP_Detailing.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HP_Detailing.Extensions;
using Microsoft.AspNetCore.SignalR;
using HP_Detailing.Hubs;

namespace HP_Detailing.Controllers
{
    // ========================================================
    // CONTROLLER: TicketsController
    // CHỨC NĂNG: Quản lý Phiếu tiếp nhận dịch vụ và điều hành tiến độ thi công
    // BẢO MẬT: Bắt buộc đăng nhập hệ thống mới được phép xem/sửa thông tin
    // ========================================================
    [Authorize(Roles = "Admin, ThuNgan")]
    public class TicketsController : Controller
    {
        private readonly HP_Detailing.Data.HP_DetailingDbContext _context;
        private readonly ILogger<TicketsController> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public TicketsController(HP_Detailing.Data.HP_DetailingDbContext context, ILogger<TicketsController> logger, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        // ========================================================
        // ACTION: Index (Danh sách phiếu)
        // NGHIỆP VỤ: Đọc live toàn bộ phiếu dịch vụ từ CSDL SQL Server, tính động tổng tiền
        // BẮT LỖI: Try-catch bao bọc kỹ để tránh treo trang
        // ========================================================
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                var ticketsFromDb = _context.Tickets
                    .Include(t => t.TicketServices)
                    .Include(t => t.AssignedStaff)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(10)
                    .ToList();

                var vm = new TicketIndexViewModel
                {
                    Tickets = ticketsFromDb.Select(t => new TicketItemViewModel
                    {
                        Id = t.Id,
                        TicketCode = t.TicketCode,
                        CustomerName = t.CustomerName,
                        CustomerPhone = t.CustomerPhone,
                        Plate = t.Plate,
                        CarModel = t.CarModel,
                        Status = t.Status,
                        AssignedStaffName = t.AssignedStaff != null ? t.AssignedStaff.FullName : null,
                        CreatedAt = t.CreatedAt,
                        // Cộng tổng tiền snapshot của tất cả dịch vụ đi kèm phiếu này
                        TotalPrice = t.TicketServices != null ? t.TicketServices.Sum(ts => ts.PriceSnapshot) : 0
                    }).ToList()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI LẤY DANH SÁCH PHIẾU DỊCH VỤ");
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> List(int page = 1, int pageSize = 10, string? search = null, string? status = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Tickets
                    .AsNoTracking()
                    .Include(t => t.TicketServices)
                    .Include(t => t.AssignedStaff)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var keyword = search.Trim();
                    query = query.Where(t =>
                        t.TicketCode.Contains(keyword) ||
                        (t.CustomerName != null && t.CustomerName.Contains(keyword)) ||
                        (t.CustomerPhone != null && t.CustomerPhone.Contains(keyword)) ||
                        (t.Plate != null && t.Plate.Contains(keyword)));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(t => t.Status == status);
                }

                query = query.OrderByDescending(t => t.CreatedAt);
                var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);

                var items = paged.Items.Select(t => new
                {
                    id = t.Id,
                    ticketCode = t.TicketCode,
                    customerName = t.CustomerName,
                    customerPhone = t.CustomerPhone,
                    plate = t.Plate,
                    carModel = t.CarModel,
                    status = t.Status,
                    assignedStaffName = t.AssignedStaff != null ? t.AssignedStaff.FullName : null,
                    createdAt = t.CreatedAt,
                    totalPrice = t.TicketServices != null ? t.TicketServices.Sum(ts => ts.PriceSnapshot) : 0m
                }).ToList();

                return Json(new
                {
                    items,
                    paged.TotalCount,
                    paged.Page,
                    paged.PageSize,
                    paged.TotalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI TẢI DANH SÁCH PHIẾU THEO TRANG");
                return BadRequest(new { message = "Không thể tải danh sách phiếu." });
            }
        }

        // ========================================================
        // ACTION: Detail (Chi tiết phiếu)
        // NGHIỆP VỤ: Eager loading nạp thông tin xe + dịch vụ đã chọn + dịch vụ có sẵn để thêm nhanh
        // BẮT LỖI: Tránh truyền tham số sai kiểu, kiểm tra null trước khi map
        // ========================================================
        [HttpGet]
        [Route("tickets/{id:int}")]
        public IActionResult Detail(int id)
        {
            try
            {
                var ticket = _context.Tickets
                    .Include(t => t.TicketServices)
                        .ThenInclude(ts => ts.Service)
                    .Include(t => t.AssignedStaff)
                    .FirstOrDefault(t => t.Id == id);

                if (ticket == null)
                {
                    return NotFound("Không tìm thấy phiếu dịch vụ yêu cầu!");
                }

                // Lấy tất cả dịch vụ đang kích hoạt trong hệ thống để chọn thêm
                var activeServices = _context.Services
                    .Where(s => s.IsActive)
                    .Select(s => new ServiceSelectViewModel
                    {
                        Id = s.Id,
                        ServiceCode = s.ServiceCode,
                        Name = s.Name,
                        UnitPrice = s.UnitPrice
                    })
                    .ToList();

                // Lấy danh sách vật tư đã xuất kho kèm theo phiếu
                var materialsUsed = _context.TicketMaterialUsages
                    .Include(mu => mu.Material)
                    .Where(mu => mu.TicketId == id)
                    .Select(mu => new TicketMaterialUsageItemViewModel
                    {
                        Id = mu.Id,
                        MaterialCode = mu.Material != null ? mu.Material.MaterialCode : "VT",
                        MaterialName = mu.Material != null ? mu.Material.Name : "Vật tư tự do",
                        Quantity = mu.Quantity,
                        Unit = mu.Material != null ? mu.Material.Unit : "Đơn vị",
                        UnitPrice = mu.UnitPrice,
                        IsChargedToCustomer = mu.IsChargedToCustomer
                    })
                    .ToList();

                var vm = new TicketDetailViewModel
                {
                    Id = ticket.Id,
                    TicketCode = ticket.TicketCode,
                    CustomerName = ticket.CustomerName,
                    CustomerPhone = ticket.CustomerPhone,
                    Plate = ticket.Plate,
                    CarModel = ticket.CarModel,
                    Status = ticket.Status,
                    AssignedStaffName = ticket.AssignedStaff != null ? ticket.AssignedStaff.FullName : null,
                    CreatedAt = ticket.CreatedAt,
                    AvailableServices = activeServices,
                    Materials = materialsUsed,
                    Services = ticket.TicketServices != null 
                        ? ticket.TicketServices.Select(ts => new TicketServiceItemViewModel
                        {
                            Id = ts.Id,
                            ServiceCode = ts.Service != null ? ts.Service.ServiceCode : "KHACH",
                            Name = ts.Service != null ? ts.Service.Name : "Dịch vụ tự chọn",
                            Price = ts.PriceSnapshot,
                            Status = ts.Status
                        }).ToList()
                        : new List<TicketServiceItemViewModel>(),
                    // Tính tổng tiền hóa đơn mẫu = Tổng tiền công + Tổng vật tư tính phí
                    TotalAmount = (ticket.TicketServices != null ? ticket.TicketServices.Sum(ts => ts.PriceSnapshot) : 0) +
                                  materialsUsed.Where(m => m.IsChargedToCustomer).Sum(m => m.Quantity * m.UnitPrice)
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI TẢI TRANG CHI TIẾT PHIẾU DỊCH VỤ ID: {Id}", id);
                return RedirectToAction("Error", "Home");
            }
        }

        // ========================================================
        // AJAX API: AddService (Thêm dịch vụ phát sinh)
        // NGHIỆP VỤ: Đọc Service từ DB -> Tạo mới TicketService -> Lưu -> Trả JSON
        // BẮT LỖI: Try-catch chống crash khi gọi AJAX SPA ngầm
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> AddService([FromBody] AddServiceRequest request)
        {
            try
            {
                if (request == null || request.TicketId <= 0 || request.ServiceId <= 0)
                    return Json(new { success = false, message = "Dữ liệu truyền lên không hợp lệ!" });

                var ticket = _context.Tickets.Find(request.TicketId);
                var service = _context.Services.Find(request.ServiceId);

                if (ticket == null || service == null)
                    return Json(new { success = false, message = "Không tìm thấy Phiếu hoặc Dịch vụ phù hợp!" });

                var ticketService = new TicketService
                {
                    TicketId = ticket.Id,
                    ServiceId = service.Id,
                    PriceSnapshot = service.UnitPrice,
                    Status = "not_started"
                };
                _context.TicketServices.Add(ticketService);
                await _context.SaveChangesAsync();

                var warnings = new List<string>();
                HP_Detailing.Data.TicketMaterialService.ApplyQuotasForService(
                    _context, ticket.Id, service.Id, warnings);
                await _context.SaveChangesAsync();

                var totalAmount = HP_Detailing.Data.InvoiceSync.SyncFromTicket(_context, ticket.Id);

                // Gửi thông báo phát sinh dịch vụ
                await NotificationHelper.SendNotificationAsync(
                    _context,
                    _hubContext,
                    "Dịch vụ bổ sung",
                    $"Phiếu {ticket.TicketCode} của khách hàng {ticket.CustomerName} đã được thêm dịch vụ: {service.Name}.",
                    "Ticket",
                    $"/tickets/{ticket.Id}"
                );

                return Json(new
                {
                    success = true,
                    message = "Thêm dịch vụ và vật tư định mức thành công! Kho đã được trừ tự động.",
                    warnings,
                    newService = new { id = ticketService.Id, code = service.ServiceCode, name = service.Name, price = service.UnitPrice, status = "not_started" },
                    totalAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI AJAX THÊM DỊCH VỤ PHÁT SINH");
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // ========================================================
        // AJAX API: UpdateStatus (Cập nhật tiến trình thi công)
        // NGHIỆP VỤ: Đổi trạng thái phiếu và ghi nhận CSDL
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            try
            {
                if (request == null || request.TicketId <= 0 || string.IsNullOrEmpty(request.Status))
                {
                    return Json(new { success = false, message = "Dữ liệu trạng thái không hợp lệ!" });
                }

                var ticket = _context.Tickets.Find(request.TicketId);
                if (ticket == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu dịch vụ!" });
                }

                ticket.Status = request.Status;
                await _context.SaveChangesAsync();

                int invoiceId = 0;
                if (request.Status == "completed")
                {
                    HP_Detailing.Data.InvoiceSync.SyncFromTicket(_context, ticket.Id);
                    var invoice = _context.Invoices.FirstOrDefault(i => i.TicketId == ticket.Id);
                    if (invoice != null) invoiceId = invoice.Id;
                }

                string statusText = request.Status == "completed" ? "đã hoàn thành" : (request.Status == "in_progress" ? "đang thi công" : request.Status);
                await NotificationHelper.SendNotificationAsync(
                    _context,
                    _hubContext,
                    "Cập nhật trạng thái phiếu",
                    $"Phiếu tiếp nhận {ticket.TicketCode} của khách hàng {ticket.CustomerName} đã chuyển sang trạng thái: {statusText}.",
                    "Ticket",
                    $"/tickets/{ticket.Id}"
                );

                return Json(new { success = true, message = "Cập nhật tiến độ thi công thành công!", invoiceId = invoiceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI AJAX CẬP NHẬT TRẠNG THÁI TIẾN ĐỘ");
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // ========================================================
        // ACTION: Create (GET - Hiển thị form tạo mới)
        // NGHIỆP VỤ: Nạp live danh sách Dịch vụ, Vật tư trong kho, Nhân sự từ SQL Server.
        //           Nếu có tham số appointmentId, tự động điền thông tin lịch hẹn.
        // ========================================================
        [HttpGet]
        public IActionResult Create(int? appointmentId, string? plate)
        {
            try
            {
                var vm = new TicketCreateViewModel();

                // 1. Nếu có lịch hẹn trước, tự động tải thông tin để Auto-fill lên form
                if (appointmentId.HasValue && appointmentId.Value > 0)
                {
                    var apt = _context.Appointments.Find(appointmentId.Value);
                    if (apt != null)
                    {
                        vm.AppointmentId = apt.Id;
                        vm.CustomerName = apt.CustomerName;
                        vm.CustomerPhone = apt.CustomerPhone;
                        vm.Plate = apt.Plate;
                        vm.SuggestedServices = apt.Services;
                        vm.PreselectedServiceIds = ResolveServiceIdsFromAppointmentText(apt.Services);
                    }
                }

                // 1b. Từ trang Hồ sơ xe: điền biển số + chủ xe từ bảng Cars
                if (string.IsNullOrEmpty(vm.Plate) && !string.IsNullOrWhiteSpace(plate))
                {
                    var plateNorm = plate.Trim().ToUpper();
                    vm.Plate = plateNorm;
                    var car = _context.Cars.FirstOrDefault(c => c.Plate == plateNorm);
                    if (car != null)
                    {
                        vm.CustomerName = car.OwnerName;
                        vm.CustomerPhone = car.OwnerPhone;
                    }
                }

                // 2. Nạp dịch vụ + định mức vật tư từ ServiceMaterialQuotas (CSDL)
                var servicesFromDb = _context.Services
                    .Include(s => s.ServiceCategory)
                    .Where(s => s.IsActive)
                    .ToList();

                var quotasByServiceId = _context.ServiceMaterialQuotas
                    .Include(q => q.Material)
                    .Where(q => q.Material != null)
                    .ToList()
                    .GroupBy(q => q.ServiceId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                vm.Services = servicesFromDb.Select(s => new ServiceCreateItemViewModel
                {
                    Id = s.Id,
                    Code = s.ServiceCode,
                    Name = s.Name,
                    Category = s.ServiceCategory != null ? s.ServiceCategory.Name : "Dịch vụ",
                    DurationMinutes = s.DurationMinutes,
                    Price = s.UnitPrice,
                    Materials = quotasByServiceId.TryGetValue(s.Id, out var serviceQuotas)
                        ? serviceQuotas.Select(q => new ServiceMaterialCreateViewModel
                        {
                            MaterialId = q.MaterialId,
                            MaterialCode = q.Material!.MaterialCode,
                            DefaultQty = q.DefaultQty
                        }).ToList()
                        : new List<ServiceMaterialCreateViewModel>()
                }).ToList();

                // 3. Nạp live Danh sách Vật tư trong kho và số lượng tồn hiện có (Warehouse Stock)
                var materialsFromDb = _context.Materials
                    .Where(m => m.IsActive)
                    .ToList();

                var stocksFromDb = _context.WarehouseStocks.ToList();

                vm.Materials = materialsFromDb.Select(m => {
                    var stock = stocksFromDb.FirstOrDefault(s => s.MaterialId == m.Id);
                    return new MaterialCreateItemViewModel
                    {
                        Id = m.Id,
                        Code = m.MaterialCode,
                        Name = m.Name,
                        Unit = m.Unit ?? "Đơn vị",
                        Stock = stock != null ? stock.QuantityOnHand : 0
                    };
                }).ToList();

                // 4. Nạp live Danh sách Nhân sự (Kỹ thuật viên) để phân công
                var staffFromDb = _context.Staff
                    .Where(s => s.IsActive)
                    .ToList();

                vm.Staffs = staffFromDb.Select(s => new StaffCreateItemViewModel
                {
                    Id = s.Id,
                    Code = s.StaffCode,
                    FullName = s.FullName,
                    Position = s.Position ?? "Kỹ thuật viên"
                }).ToList();

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI KHI TẢI TRANG TẠO PHIẾU DỊCH VỤ MỚI");
                return RedirectToAction("Error", "Home");
            }
        }

        // ========================================================
        // AJAX API: CreateAjax (Lưu phiếu dịch vụ mới qua AJAX)
        // NGHIỆP VỤ: Lưu Xe -> Lưu Ticket -> Lưu TicketServices -> Lưu TicketMaterialUsages -> Trừ Kho -> Tạo Invoice UNPAID
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] CreateTicketAjaxRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.CustomerName) || string.IsNullOrEmpty(request.CustomerPhone) || string.IsNullOrEmpty(request.Plate))
                {
                    return Json(new { success = false, message = "Thông tin Khách hàng, Số điện thoại và Biển số xe không được để trống!" });
                }

                if (request.Services == null || request.Services.Count == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn ít nhất một dịch vụ để tiếp nhận xe!" });
                }

                // 1. Quản lý hoặc tạo mới thực thể Xe (Car) bằng Khóa chính Plate (Biển số)
                var plate = request.Plate.Trim().ToUpper();
                var car = _context.Cars.FirstOrDefault(c => c.Plate == plate);
                if (car == null)
                {
                    car = new Car
                    {
                        Plate = plate,
                        Brand = request.CarBrand ?? "Hãng khác",
                        Model = request.CarModel ?? "Dòng khác",
                        Color = request.CarColor ?? "Chưa rõ",
                        OwnerName = request.CustomerName,
                        OwnerPhone = request.CustomerPhone,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Cars.Add(car);
                }
                else
                {
                    car.Brand = request.CarBrand ?? car.Brand;
                    car.Model = request.CarModel ?? car.Model;
                    car.Color = request.CarColor ?? car.Color;
                    car.OwnerName = request.CustomerName ?? car.OwnerName;
                    car.OwnerPhone = request.CustomerPhone ?? car.OwnerPhone;
                }

                // 2. Tự động sinh mã phiếu tiếp nhận thông minh PDV-YYMMDD-[Index]
                var dateStr = DateTime.UtcNow.ToString("yyMMdd");
                var countToday = _context.Tickets.Count(t => t.TicketCode.Contains("PDV" + dateStr)) + 1;
                var ticketCode = $"PDV{dateStr}-{countToday:D3}";

                var newTicket = new Ticket
                {
                    TicketCode = ticketCode,
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone,
                    Plate = plate,
                    CarModel = request.CarModel,
                    AssignedStaffId = request.AssignedStaffId,
                    CreatedAt = DateTime.UtcNow,
                    Status = "pending"
                };

                _context.Tickets.Add(newTicket);
                await _context.SaveChangesAsync(); // Lưu lấy ID Ticket

                // 3. Lưu từng dịch vụ (tiền công)
                foreach (var svcReq in request.Services)
                {
                    var service = _context.Services.Find(svcReq.ServiceId);
                    if (service == null) continue;

                    var ticketService = new TicketService
                    {
                        TicketId = newTicket.Id,
                        ServiceId = service.Id,
                        PriceSnapshot = service.UnitPrice,
                        Status = "not_started"
                    };

                    _context.TicketServices.Add(ticketService);
                }

                var warnings = new List<string>();

                var serviceIds = request.Services.Select(s => s.ServiceId).ToList();
                var quotaOverrides = (request.Materials ?? new List<SelectedMaterialRequest>())
                    .Where(m => !m.IsExtra)
                    .Select(m => new HP_Detailing.Data.TicketMaterialService.MaterialLine
                    {
                        MaterialId = m.MaterialId,
                        Quantity = m.Qty,
                        UnitPrice = m.UnitPrice,
                        IsChargedToCustomer = m.IsChargedToCustomer
                    })
                    .ToList();

                var extras = (request.Materials ?? new List<SelectedMaterialRequest>())
                    .Where(m => m.IsExtra)
                    .Select(m => new HP_Detailing.Data.TicketMaterialService.MaterialLine
                    {
                        MaterialId = m.MaterialId,
                        Quantity = m.Qty,
                        UnitPrice = m.UnitPrice,
                        IsChargedToCustomer = m.IsChargedToCustomer
                    })
                    .ToList();

                // 4a. Định mức từ CSDL (giống AddService), SL/giá lấy từ form nếu user chỉnh
                HP_Detailing.Data.TicketMaterialService.ApplyQuotasForServices(
                    _context, newTicket.Id, serviceIds, quotaOverrides, warnings);

                // 4b. Vật tư phụ thêm ngoài định mức
                HP_Detailing.Data.TicketMaterialService.ApplyExtraMaterials(
                    _context, newTicket.Id, extras, warnings);

                await _context.SaveChangesAsync();

                // 5. Nếu phiếu này tạo từ lịch hẹn, chuyển lịch sang arrived
                if (request.AppointmentId.HasValue && request.AppointmentId.Value > 0)
                {
                    var apt = _context.Appointments.Find(request.AppointmentId.Value);
                    if (apt != null) apt.Status = "arrived";
                }

                await _context.SaveChangesAsync();

                // 6. Tạo / đồng bộ hóa đơn UNPAID + chi tiết InvoiceService
                HP_Detailing.Data.InvoiceSync.SyncFromTicket(_context, newTicket.Id);

                // Gửi thông báo realtime khi tạo phiếu tiếp nhận mới
                await NotificationHelper.SendNotificationAsync(
                    _context,
                    _hubContext,
                    "Phiếu tiếp nhận mới",
                    $"Phiếu tiếp nhận {newTicket.TicketCode} của khách hàng {newTicket.CustomerName} ({newTicket.Plate}) đã được tạo.",
                    "Ticket",
                    $"/tickets/{newTicket.Id}"
                );

                return Json(new { success = true, message = "Tạo phiếu dịch vụ và xuất kho vật tư thành công!", warnings, redirectUrl = "/tickets" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI KHI LƯU PHIẾU DỊCH VỤ MỚI QUA AJAX");
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult New() => RedirectToAction(nameof(Create));

        // GET /tickets/check-car
        [HttpGet("tickets/check-car")]
        public IActionResult CheckCar(string plate)
        {
            if (string.IsNullOrEmpty(plate))
                return Json(new { found = false });

            var car = _context.Cars.FirstOrDefault(c => c.Plate == plate.Trim().ToUpper());
            if (car == null)
                return Json(new { found = false });

            return Json(new
            {
                found = true,
                brand = car.Brand,
                model = car.Model,
                color = car.Color,
                ownerName = car.OwnerName,
                ownerPhone = car.OwnerPhone
            });
        }

        // GET /tickets/service-quotas
        [HttpGet("tickets/service-quotas")]
        public IActionResult GetServiceQuotas([FromQuery] List<int> serviceIds)
        {
            if (serviceIds == null || !serviceIds.Any())
                return Json(new List<object>());

            var stockByMaterial = _context.WarehouseStocks
                .AsNoTracking()
                .ToDictionary(s => s.MaterialId, s => s.QuantityOnHand);

            var quotas = _context.ServiceMaterialQuotas
                .Include(q => q.Material)
                .Where(q => serviceIds.Contains(q.ServiceId))
                .AsEnumerable()
                .Select(q => new
                {
                    q.ServiceId,
                    q.MaterialId,
                    materialName = q.Material != null ? q.Material.Name : "",
                    materialCode = q.Material != null ? q.Material.MaterialCode : "",
                    unit = q.Material != null ? q.Material.Unit : "",
                    unitPrice = q.Material != null ? q.Material.UnitPrice : 0,
                    qty = q.DefaultQty,
                    stockOnHand = stockByMaterial.GetValueOrDefault(q.MaterialId, 0m)
                })
                .ToList();

            return Json(quotas);
        }

        /// <summary>Khớp chuỗi dịch vụ lịch hẹn (tên hoặc mã) với danh sách Service trong DB.</summary>
        private List<int> ResolveServiceIdsFromAppointmentText(string? appointmentServices)
        {
            if (string.IsNullOrWhiteSpace(appointmentServices))
                return new List<int>();

            var tokens = appointmentServices
                .Split(new[] { ',', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length > 0)
                .ToList();

            if (tokens.Count == 0)
                tokens.Add(appointmentServices.Trim());

            var allServices = _context.Services
                .Where(s => s.IsActive)
                .Select(s => new { s.Id, s.ServiceCode, s.Name })
                .ToList();

            var ids = new List<int>();
            foreach (var token in tokens)
            {
                var t = token.Trim();
                var match = allServices.FirstOrDefault(s =>
                    s.Name.Equals(t, StringComparison.OrdinalIgnoreCase)
                    || s.ServiceCode.Equals(t, StringComparison.OrdinalIgnoreCase)
                    || s.Name.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || t.Contains(s.Name, StringComparison.OrdinalIgnoreCase));

                if (match != null && !ids.Contains(match.Id))
                    ids.Add(match.Id);
            }

            return ids;
        }
    } // end TicketsController

    // ========================================================
    // LỚP REQUEST DTO HỖ TRỢ TRUYỀN DỮ LIỆU AJAX
    // ========================================================
    public class CreateTicketAjaxRequest
    {
        public int? AppointmentId { get; set; }
        public int? AssignedStaffId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string Plate { get; set; } = string.Empty;
        public string? CarBrand { get; set; }
        public string? CarModel { get; set; }
        public string? CarColor { get; set; }
        public List<SelectedServiceRequest> Services { get; set; } = new();
        public List<SelectedMaterialRequest> Materials { get; set; } = new();
    }

    public class SelectedServiceRequest
    {
        public int ServiceId { get; set; }
    }

    public class SelectedMaterialRequest
    {
        public int MaterialId { get; set; }
        public decimal Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public bool IsChargedToCustomer { get; set; } = true;
        /// <summary>true = vật tư thêm thủ công, không từ định mức dịch vụ.</summary>
        public bool IsExtra { get; set; }
    }

    public class AddServiceRequest
    {
        public int TicketId { get; set; }
        public int ServiceId { get; set; }
    }

    public class UpdateStatusRequest
    {
        public int TicketId { get; set; }
        public string Status { get; set; }
    }
}
