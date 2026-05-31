using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HP_Detailing.Models;
using HP_Detailing.Models.ViewModels;

namespace HP_Detailing.Controllers
{
    // ========================================================
    // CONTROLLER: FinancialController
    // CHỨC NĂNG: Quản lý Hóa đơn & Thanh toán – Đọc live từ SQL Server
    // BẢO MẬT: Bắt buộc đăng nhập
    // ========================================================
    [Authorize(Roles = "Admin, ThuNgan")]
    public class FinancialController : Controller
    {
        private readonly HP_Detailing.Data.HP_DetailingDbContext _context;

        private List<PaymentMethod> GetActivePaymentMethods()
        {
            var methods = _context.PaymentMethods
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.BankShortName)
                .ToList();

            if (methods.Count == 0)
            {
                methods = new List<PaymentMethod>
                {
                    new PaymentMethod { BankFullName = "Ngân hàng Thương mại Cổ phần Ngoại thương Việt Nam", BankShortName = "Vietcombank", AccountNumber = "1012345678", Owner = "CÔNG TY HP DETAILING", IsDefault = true, IsActive = true },
                    new PaymentMethod { BankFullName = "Ngân hàng TMCP Kỹ thương Việt Nam", BankShortName = "Techcombank", AccountNumber = "190333444555", Owner = "CÔNG TY HP DETAILING", IsDefault = false, IsActive = true },
                    new PaymentMethod { BankFullName = "Ví điện tử MoMo Business", BankShortName = "Ví MoMo", AccountNumber = "0909876543", Owner = "VŨ ĐỨC TRỌNG", IsDefault = false, IsActive = true },
                    new PaymentMethod { BankFullName = "Thanh toán bằng Tiền mặt tại Quầy", BankShortName = "Tiền mặt", AccountNumber = "", Owner = "Thu ngân HP", IsDefault = false, IsActive = true }
                };

                _context.PaymentMethods.AddRange(methods);
                _context.SaveChanges();
            }

            return methods;
        }

        public FinancialController(HP_Detailing.Data.HP_DetailingDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // ACTION: Index (Danh sách Hóa đơn live từ DB)
        // ========================================================
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                // Lấy tất cả hóa đơn kèm theo thông tin Phiếu dịch vụ liên kết
                var invoicesFromDb = _context.Invoices
                    .Include(i => i.Ticket)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToList();

                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);

                var vm = new FinancialIndexViewModel
                {
                    // Tổng doanh thu tháng này (chỉ tính hóa đơn PAID)
                    MonthlyRevenue = invoicesFromDb
                        .Where(i => i.Status == "PAID" && (i.PaidAt ?? i.CreatedAt) >= startOfMonth)
                        .Sum(i => i.TotalAmount),

                    // Số hóa đơn chưa thanh toán
                    UnpaidCount = invoicesFromDb.Count(i => i.Status == "UNPAID"),

                    // Tổng giá trị chưa thanh toán
                    TotalUnpaidAmount = invoicesFromDb
                        .Where(i => i.Status == "UNPAID")
                        .Sum(i => i.TotalAmount),

                    // Số hóa đơn đã thanh toán tháng này
                    PaidCount = invoicesFromDb
                        .Count(i => i.Status == "PAID" && (i.PaidAt ?? i.CreatedAt) >= startOfMonth),

                    // Danh sách chi tiết hóa đơn
                    Invoices = invoicesFromDb.Select(i => new InvoiceItemViewModel
                    {
                        Id = i.Id,
                        InvoiceCode = i.InvoiceCode,
                        TicketCode = i.Ticket != null ? i.Ticket.TicketCode : "N/A",
                        TicketId = i.TicketId ?? 0,
                        CustomerName = i.Ticket != null ? i.Ticket.CustomerName : "N/A",
                        VehiclePlate = i.Ticket != null ? i.Ticket.Plate : "",
                        VehicleModel = i.Ticket != null ? i.Ticket.CarModel : "",
                        TotalAmount = i.TotalAmount,
                        Status = i.Status,
                        CreatedDateFormatted = i.CreatedAt.ToString("dd/MM/yyyy")
                    }).ToList()
                };

                var methods = GetActivePaymentMethods();

                ViewBag.DefaultPaymentMethodId = methods
                    .Where(p => p.IsDefault)
                    .Select(p => (int?)p.Id)
                    .FirstOrDefault()
                    ?? methods.Select(p => (int?)p.Id).FirstOrDefault();

                return View(vm);
            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Home");
            }
        }

        // ========================================================
        // ACTION: Invoice (Chi tiết hóa đơn từ DB)
        // ========================================================
        [HttpGet]
        public IActionResult Invoice(int id)
        {
            try
            {
                var invoice = _context.Invoices
                    .Include(i => i.PaymentMethod)
                    .Include(i => i.Ticket)
                        .ThenInclude(t => t!.TicketServices)
                            .ThenInclude(ts => ts.Service)
                    .Include(i => i.Ticket)
                        .ThenInclude(t => t!.TicketMaterialUsages)
                            .ThenInclude(tm => tm.Material)
                    .FirstOrDefault(i => i.Id == id);

                if (invoice == null)
                    return NotFound("Không tìm thấy hóa đơn yêu cầu!");

                ViewBag.PaymentMethods = GetActivePaymentMethods();

                ViewData["InvoiceId"] = invoice.InvoiceCode;
                return View(invoice);
            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Home");
            }
        }

        // ========================================================
        // AJAX API: MarkPaid (Xác nhận thanh toán)
        // ========================================================
        [HttpPost]
        public IActionResult MarkPaid([FromBody] MarkPaidRequest request)
        {
            try
            {
                if (request == null || request.InvoiceId <= 0)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });

                var invoice = _context.Invoices.Find(request.InvoiceId);
                if (invoice == null)
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn!" });

                if (invoice.Status == "PAID")
                    return Json(new { success = false, message = "Hóa đơn đã được thanh toán trước đó." });

                if (invoice.Status == "CANCELLED")
                    return Json(new { success = false, message = "Không thể thanh toán hóa đơn đã hủy." });

                var methods = GetActivePaymentMethods();

                PaymentMethod? method = null;
                if (request.PaymentMethodId > 0)
                {
                    method = methods.FirstOrDefault(p => p.Id == request.PaymentMethodId && p.IsActive);
                    if (method == null)
                        return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ." });
                }
                else
                {
                    method = methods.FirstOrDefault(p => p.IsDefault && p.IsActive)
                        ?? methods.FirstOrDefault(p => p.IsActive);
                    if (method == null)
                        return Json(new { success = false, message = "Chưa cấu hình phương thức thanh toán trong hệ thống." });
                }

                invoice.Status = "PAID";
                invoice.PaidAt = DateTime.UtcNow;
                invoice.PaymentMethodId = method.Id;
                invoice.PaymentNote = string.IsNullOrWhiteSpace(request.PaymentNote) ? null : request.PaymentNote.Trim();
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Xác nhận thanh toán thành công!",
                    paidAt = invoice.PaidAt.Value.ToString("dd/MM/yyyy HH:mm"),
                    paymentMethod = method.BankShortName
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }

    // DTO cho AJAX
    public class MarkPaidRequest
    {
        public int InvoiceId { get; set; }
        public int PaymentMethodId { get; set; }
        public string? PaymentNote { get; set; }
    }
}
