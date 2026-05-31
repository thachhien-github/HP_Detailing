using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HP_Detailing.Data;
using HP_Detailing.Models;
using System.Linq;
using System;

namespace HP_Detailing.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class PositionsController : Controller
    {
        private readonly HP_DetailingDbContext _context;

        public PositionsController(HP_DetailingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách tất cả vị trí / chức vụ
        /// GET: api/positions
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            try
            {
                var positions = _context.Positions
                    .Include(p => p.Staffs)
                    .OrderBy(p => p.PositionCode)
                    .ToList();

                var result = positions.Select(p => new
                {
                    p.Id,
                    p.PositionCode,
                    p.Name,
                    p.Description,
                    StaffCount = p.Staffs?.Count ?? 0
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết một vị trí theo ID
        /// GET: api/positions/{id}
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetById(int id)
        {
            try
            {
                var position = _context.Positions
                    .Include(p => p.Staffs)
                    .FirstOrDefault(p => p.Id == id);

                if (position == null)
                    return Json(new { success = false, message = "Vị trí không tồn tại." });

                object staffData = position.Staffs != null 
                    ? position.Staffs.Select(s => new { s.Id, s.StaffCode, s.FullName }).ToList()
                    : new List<dynamic>();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        position.Id,
                        position.PositionCode,
                        position.Name,
                        position.Description,
                        Staffs = staffData
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Thêm vị trí / chức vụ mới
        /// POST: api/positions
        /// </summary>
        [HttpPost]
        public IActionResult Create([FromBody] CreatePositionRequest request)
        {
            try
            {
                // Kiểm tra dữ liệu bắt buộc
                if (string.IsNullOrWhiteSpace(request.PositionCode) || string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new { success = false, message = "Mã chức vụ và tên chức vụ là bắt buộc." });
                }

                // Kiểm tra mã chức vụ không trùng lặp
                if (_context.Positions.Any(p => p.PositionCode == request.PositionCode))
                {
                    return Json(new { success = false, message = $"Mã chức vụ '{request.PositionCode}' đã tồn tại." });
                }

                var position = new Position
                {
                    PositionCode = request.PositionCode.Trim(),
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim()
                };

                _context.Positions.Add(position);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Thêm vị trí '{position.Name}' thành công.",
                    data = new { position.Id, position.PositionCode, position.Name, position.Description }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreatePosition Error: {ex}");
                return Json(new { success = false, message = $"Lỗi: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        /// <summary>
        /// Cập nhật thông tin vị trí / chức vụ
        /// PUT: api/positions/{id}
        /// </summary>
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] UpdatePositionRequest request)
        {
            try
            {
                var position = _context.Positions.FirstOrDefault(p => p.Id == id);
                if (position == null)
                {
                    return Json(new { success = false, message = "Vị trí không tồn tại." });
                }

                // Kiểm tra dữ liệu bắt buộc
                if (string.IsNullOrWhiteSpace(request.PositionCode) || string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new { success = false, message = "Mã chức vụ và tên chức vụ là bắt buộc." });
                }

                // Kiểm tra mã chức vụ không trùng lặp (trừ chính nó)
                if (position.PositionCode != request.PositionCode && 
                    _context.Positions.Any(p => p.PositionCode == request.PositionCode))
                {
                    return Json(new { success = false, message = $"Mã chức vụ '{request.PositionCode}' đã tồn tại." });
                }

                position.PositionCode = request.PositionCode.Trim();
                position.Name = request.Name.Trim();
                position.Description = request.Description?.Trim();

                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Cập nhật vị trí '{position.Name}' thành công.",
                    data = new { position.Id, position.PositionCode, position.Name, position.Description }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePosition Error: {ex}");
                return Json(new { success = false, message = $"Lỗi: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        /// <summary>
        /// Xóa vị trí / chức vụ
        /// DELETE: api/positions/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var position = _context.Positions
                    .Include(p => p.Staffs)
                    .FirstOrDefault(p => p.Id == id);

                if (position == null)
                {
                    return Json(new { success = false, message = "Vị trí không tồn tại." });
                }

                // Kiểm tra xem vị trí này có nhân viên không
                if (position.Staffs != null && position.Staffs.Any())
                {
                    return Json(new 
                    { 
                        success = false, 
                        message = $"Không thể xóa vị trí '{position.Name}' vì có {position.Staffs.Count} nhân viên đang giữ vị trí này. Vui lòng chuyển nhân viên sang vị trí khác trước." 
                    });
                }

                _context.Positions.Remove(position);
                _context.SaveChanges();

                return Json(new { success = true, message = $"Xóa vị trí '{position.Name}' thành công." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeletePosition Error: {ex}");
                return Json(new { success = false, message = $"Lỗi: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        /// <summary>
        /// Bulk create positions - thêm nhiều vị trí cùng một lúc
        /// POST: api/positions/bulk
        /// </summary>
        [HttpPost("bulk")]
        public IActionResult BulkCreate([FromBody] List<CreatePositionRequest> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    return Json(new { success = false, message = "Danh sách vị trí trống." });
                }

                var created = new List<dynamic>();
                var errors = new List<string>();

                foreach (var request in requests)
                {
                    if (string.IsNullOrWhiteSpace(request.PositionCode) || string.IsNullOrWhiteSpace(request.Name))
                    {
                        errors.Add($"Bỏ qua: Mã hoặc tên chức vụ trống");
                        continue;
                    }

                    if (_context.Positions.Any(p => p.PositionCode == request.PositionCode))
                    {
                        errors.Add($"Mã chức vụ '{request.PositionCode}' đã tồn tại - bỏ qua");
                        continue;
                    }

                    var position = new Position
                    {
                        PositionCode = request.PositionCode.Trim(),
                        Name = request.Name.Trim(),
                        Description = request.Description?.Trim()
                    };

                    _context.Positions.Add(position);
                    created.Add(new { position.Id, position.PositionCode, position.Name });
                }

                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Thêm {created.Count} vị trí thành công{(errors.Any() ? $", {errors.Count} lỗi" : "")}",
                    data = new { created, errors }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BulkCreatePositions Error: {ex}");
                return Json(new { success = false, message = $"Lỗi: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        public class CreatePositionRequest
        {
            public string PositionCode { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        public class UpdatePositionRequest
        {
            public string PositionCode { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }
    }
}
