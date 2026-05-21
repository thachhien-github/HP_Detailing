using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HP_Detailing.Data;
using HP_Detailing.Models;
using HP_Detailing.Extensions;

namespace HP_Detailing.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CatalogController : Controller
    {
        private readonly HP_DetailingDbContext _context;

        public CatalogController(HP_DetailingDbContext context)
        {
            _context = context;
        }

        [HttpGet("catalog")]
        public IActionResult Index() => Redirect("/catalog/services");

        [HttpGet("catalog/services")]
        public IActionResult Services()
        {
            ViewBag.Services = _context.Services
                .Include(s => s.ServiceCategory)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToList();
            ViewBag.Categories = _context.ServiceCategories
                .OrderBy(c => c.Name)
                .ToList();
            ViewBag.ActiveTab = "services";
            return View();
        }

        [HttpGet("catalog/categories")]
        public IActionResult CategoryList()
        {
            var list = _context.ServiceCategories
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToList();
            return Json(list);
        }

        [HttpGet("catalog/services/list")]
        public async Task<IActionResult> ServiceList(int page = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Services
                .AsNoTracking()
                .Include(s => s.ServiceCategory)
                .Where(s => s.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(s =>
                    s.ServiceCode.Contains(keyword) ||
                    s.Name.Contains(keyword) ||
                    (s.ServiceCategory != null && s.ServiceCategory.Name.Contains(keyword)));
            }

            query = query.OrderByDescending(s => s.Id);
            var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);

            var items = paged.Items.Select(s => new
            {
                s.Id,
                s.ServiceCode,
                s.Name,
                s.ServiceCategoryId,
                CategoryName = s.ServiceCategory != null ? s.ServiceCategory.Name : "",
                s.DurationMinutes,
                s.UnitPrice,
                s.IsActive
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

        public class ServiceSaveRequest
        {
            public int? Id { get; set; }
            public string ServiceCode { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int ServiceCategoryId { get; set; }
            public int DurationMinutes { get; set; }
            public decimal UnitPrice { get; set; }
            public bool IsActive { get; set; } = true;
        }

        [HttpPost("catalog/services/save")]
        public IActionResult ServiceSave([FromBody] ServiceSaveRequest req)
        {
            var code = (req.ServiceCode ?? "").Trim().ToUpper();
            var name = (req.Name ?? "").Trim();

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                return Json(new { success = false, message = "Mã và tên dịch vụ không được để trống." });

            if (req.ServiceCategoryId <= 0)
                return Json(new { success = false, message = "Vui lòng chọn danh mục dịch vụ." });

            if (req.DurationMinutes <= 0)
                return Json(new { success = false, message = "Thời gian thi công phải lớn hơn 0 phút." });

            if (req.UnitPrice < 0)
                return Json(new { success = false, message = "Đơn giá không hợp lệ." });

            var categoryExists = _context.ServiceCategories.Any(c => c.Id == req.ServiceCategoryId);
            if (!categoryExists)
                return Json(new { success = false, message = "Danh mục dịch vụ không tồn tại." });

            Service service;
            if (req.Id.HasValue && req.Id > 0)
            {
                service = _context.Services.Find(req.Id.Value)!;
                if (service == null)
                    return Json(new { success = false, message = "Không tìm thấy dịch vụ." });

                var codeTaken = _context.Services.Any(s =>
                    s.Id != service.Id && s.ServiceCode.ToUpper() == code);
                if (codeTaken)
                    return Json(new { success = false, message = "Mã dịch vụ đã được sử dụng." });

                service.ServiceCode = code;
                service.Name = name;
                service.ServiceCategoryId = req.ServiceCategoryId;
                service.DurationMinutes = req.DurationMinutes;
                service.UnitPrice = req.UnitPrice;
                service.IsActive = req.IsActive;
            }
            else
            {
                if (_context.Services.Any(s => s.ServiceCode.ToUpper() == code))
                    return Json(new { success = false, message = "Mã dịch vụ đã tồn tại." });

                service = new Service
                {
                    ServiceCode = code,
                    Name = name,
                    ServiceCategoryId = req.ServiceCategoryId,
                    DurationMinutes = req.DurationMinutes,
                    UnitPrice = req.UnitPrice,
                    IsActive = true
                };
                _context.Services.Add(service);
            }

            _context.SaveChanges();
            return Json(new { success = true, id = service.Id });
        }

        [HttpPost("catalog/services/delete")]
        public IActionResult ServiceDelete([FromBody] int id)
        {
            var service = _context.Services.Find(id);
            if (service == null)
                return Json(new { success = false, message = "Không tìm thấy dịch vụ." });

            var inUse = _context.TicketServices.Any(ts => ts.ServiceId == id);
            if (inUse)
            {
                service.IsActive = false;
                _context.SaveChanges();
                return Json(new { success = true, message = "Dịch vụ đã có trong phiếu — đã chuyển sang tạm ngưng thay vì xóa." });
            }

            service.IsActive = false;
            _context.SaveChanges();
            return Json(new { success = true, message = "Đã tạm ngưng dịch vụ." });
        }

        [HttpGet("catalog/quotas")]
        public IActionResult Quotas(int? serviceId)
        {
            ViewBag.Services = _context.Services
                .Include(s => s.ServiceCategory)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToList();
            ViewBag.Materials = _context.Materials
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToList();
            ViewBag.Quotas = _context.ServiceMaterialQuotas
                .Include(q => q.Service)
                .Include(q => q.Material)
                .OrderBy(q => q.Service!.Name)
                .ThenBy(q => q.Material!.Name)
                .ToList();
            ViewBag.PreselectedServiceId = serviceId;
            ViewBag.ActiveTab = "quotas";
            return View();
        }

        [HttpGet("catalog/quotas/list")]
        [HttpGet("settings/quotas")]
        public async Task<IActionResult> QuotaList(int? serviceId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var query = _context.ServiceMaterialQuotas
                .AsNoTracking()
                .Include(q => q.Service)
                .Include(q => q.Material)
                .AsQueryable();

            if (serviceId.HasValue && serviceId.Value > 0)
            {
                query = query.Where(q => q.ServiceId == serviceId.Value);
            }

            query = query.OrderByDescending(q => q.Id);
            var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);

            var items = paged.Items.Select(q => new
            {
                q.Id,
                ServiceId = q.ServiceId,
                ServiceName = q.Service != null ? q.Service.Name : "",
                MaterialId = q.MaterialId,
                MaterialName = q.Material != null ? q.Material.Name : "",
                Unit = q.Material != null ? q.Material.Unit : "",
                q.DefaultQty,
                q.Notes
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

        public class QuotaSaveRequest
        {
            public int? Id { get; set; }
            public int ServiceId { get; set; }
            public int MaterialId { get; set; }
            public decimal DefaultQty { get; set; }
            public string? Notes { get; set; }
        }

        [HttpPost("catalog/quotas/save")]
        [HttpPost("settings/quotas/save")]
        public IActionResult QuotaSave([FromBody] QuotaSaveRequest req)
        {
            if (req.ServiceId <= 0 || req.MaterialId <= 0 || req.DefaultQty <= 0)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            ServiceMaterialQuota quota;
            if (req.Id.HasValue && req.Id > 0)
            {
                quota = _context.ServiceMaterialQuotas.Find(req.Id.Value)!;
                if (quota == null)
                    return Json(new { success = false, message = "Không tìm thấy định mức." });
                quota.DefaultQty = req.DefaultQty;
                quota.Notes = req.Notes;
                quota.UpdatedAt = DateTime.Now;
            }
            else
            {
                var exists = _context.ServiceMaterialQuotas.Any(q =>
                    q.ServiceId == req.ServiceId && q.MaterialId == req.MaterialId);
                if (exists)
                    return Json(new { success = false, message = "Định mức này đã tồn tại. Hãy chỉnh sửa định mức cũ." });

                quota = new ServiceMaterialQuota
                {
                    ServiceId = req.ServiceId,
                    MaterialId = req.MaterialId,
                    DefaultQty = req.DefaultQty,
                    Notes = req.Notes,
                    CreatedAt = DateTime.Now
                };
                _context.ServiceMaterialQuotas.Add(quota);
            }

            _context.SaveChanges();
            return Json(new { success = true, id = quota.Id });
        }

        [HttpPost("catalog/quotas/delete")]
        [HttpPost("settings/quotas/delete")]
        public IActionResult QuotaDelete([FromBody] int id)
        {
            var quota = _context.ServiceMaterialQuotas.Find(id);
            if (quota == null)
                return Json(new { success = false, message = "Không tìm thấy định mức." });
            _context.ServiceMaterialQuotas.Remove(quota);
            _context.SaveChanges();
            return Json(new { success = true });
        }
    }
}
