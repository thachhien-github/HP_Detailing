using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HP_Detailing.Data;
using HP_Detailing.Models;
using System.Linq;
using System;
using System.Collections.Generic;

namespace HP_Detailing.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StaffController : Controller
    {
        private readonly HP_DetailingDbContext _context;

        public StaffController(HP_DetailingDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var staffList = _context.Staff
                .Include(s => s.LaborContracts)
                .Include(s => s.Payrolls)
                .Include(s => s.PositionEntity)
                .OrderBy(s => s.StaffCode)
                .ToList();

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            decimal totalBasic = 0;
            decimal totalBonus = 0;
            decimal totalDeduct = 0;

            foreach (var s in staffList)
            {
                var activeContract = s.LaborContracts.FirstOrDefault(c => c.Status == "Hiệu lực");
                if (activeContract != null) totalBasic += activeContract.BasicSalary;

                var currentPayroll = s.Payrolls.FirstOrDefault(p => p.Month == currentMonth && p.Year == currentYear);
                if (currentPayroll != null)
                {
                    totalBonus += currentPayroll.Bonus;
                    totalDeduct += currentPayroll.Deduction;
                }
            }

            var recentActivity = _context.Tickets
                .Include(t => t.AssignedStaff)
                .Where(t => t.AssignedStaffId != null)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToList();

            ViewBag.TotalBasic = totalBasic;
            ViewBag.TotalBonus = totalBonus;
            ViewBag.TotalDeduct = totalDeduct;
            ViewBag.TotalPayroll = totalBasic + totalBonus - totalDeduct;
            ViewBag.RecentActivity = recentActivity;
            ViewBag.Positions = _context.Positions.OrderBy(p => p.Id).ToList();

            return View(staffList);
        }

        [HttpGet]
        public IActionResult Detail(int id)
        {
            var staff = _context.Staff
                .Include(s => s.Profile)
                .Include(s => s.LaborContracts)
                .Include(s => s.Payrolls)
                .Include(s => s.PositionEntity)
                .FirstOrDefault(s => s.Id == id);
                
            if (staff == null)
            {
                return NotFound("Không tìm thấy nhân viên.");
            }

            var recentTickets = _context.Tickets
                .Where(t => t.AssignedStaffId == id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToList();

            ViewBag.RecentTickets = recentTickets;
            ViewBag.Positions = _context.Positions.OrderBy(p => p.Id).ToList();

            return View(staff);
        }

        public class CreateStaffRequest
        {
            public string StaffCode { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public int? PositionId { get; set; }
            public string? Specialty { get; set; }
            public bool Gender { get; set; }
            public string? Address { get; set; }
            public DateTime? HireDate { get; set; }
            public decimal BasicSalary { get; set; }
        }

        [HttpPost]
        public IActionResult CreateAjax([FromBody] CreateStaffRequest model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.StaffCode) || string.IsNullOrWhiteSpace(model.FullName))
                {
                    return Json(new { success = false, message = "Mã NV và Họ Tên là bắt buộc." });
                }

                if (_context.Staff.Any(s => s.StaffCode == model.StaffCode))
                {
                    return Json(new { success = false, message = "Mã nhân viên đã tồn tại." });
                }

                // Lookup Position name từ DB
                string? positionName = null;
                if (model.PositionId.HasValue)
                {
                    positionName = _context.Positions
                        .Where(p => p.Id == model.PositionId.Value)
                        .Select(p => p.Name)
                        .FirstOrDefault();
                }

                var newStaff = new Staff
                {
                    StaffCode = model.StaffCode,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Gender = model.Gender,
                    Address = model.Address,
                    HireDate = model.HireDate ?? DateTime.Now,
                    PositionId = model.PositionId,
                    Position = positionName,
                    Specialty = model.Specialty,
                    IsActive = true,
                    Status = "Hoạt động"
                };

                _context.Staff.Add(newStaff);
                _context.SaveChanges(); // ← Lưu Staff trước để tạo ID

                // Bây giờ newStaff.Id đã có giá trị
                var profile = new StaffProfile { StaffId = newStaff.Id };
                _context.StaffProfiles.Add(profile);

                var contract = new LaborContract
                {
                    ContractCode = $"HDLD-{model.StaffCode}-{DateTime.Now:yyMM}",
                    StaffId = newStaff.Id,
                    StartDate = newStaff.HireDate,
                    BasicSalary = model.BasicSalary > 0 ? model.BasicSalary : 0,
                    ContractType = "Thử việc / Toàn thời gian",
                    Status = "Hiệu lực"
                };
                _context.LaborContracts.Add(contract);

                _context.SaveChanges(); // Lưu Profile & Contract

                return Json(new { success = true, data = newStaff });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateAjax Error: {ex}");
                return Json(new { success = false, message = $"Lỗi: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        public class UpdateProfileRequest
        {
            public int StaffId { get; set; }
            public string StaffCode { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public int? PositionId { get; set; }
            public string? Specialty { get; set; }
            public string? Address { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public bool Gender { get; set; }
            public DateTime? HireDate { get; set; }

            // Profile
            public string? IdentityCard { get; set; }
            public DateTime? IssueDate { get; set; }
            public string? IssuePlace { get; set; }
            public string? Ethnicity { get; set; }
            public string? Religion { get; set; }
            public bool MaritalStatus { get; set; }
        }

        [HttpPost]
        public IActionResult UpdateFullProfileAjax([FromBody] UpdateProfileRequest model)
        {
            try
            {
                var staff = _context.Staff.Include(s => s.Profile).FirstOrDefault(s => s.Id == model.StaffId);
                if (staff == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy nhân viên." });
                }

                if (staff.StaffCode != model.StaffCode && _context.Staff.Any(s => s.StaffCode == model.StaffCode))
                {
                    return Json(new { success = false, message = "Mã nhân viên mới đã tồn tại." });
                }

                // Update Staff
                staff.StaffCode = model.StaffCode;
                staff.FullName = model.FullName;
                staff.Phone = model.Phone;
                staff.Specialty = model.Specialty;
                staff.PositionId = model.PositionId;
                // Sync Position string từ bảng Positions
                if (model.PositionId.HasValue)
                {
                    staff.Position = _context.Positions
                        .Where(p => p.Id == model.PositionId.Value)
                        .Select(p => p.Name)
                        .FirstOrDefault();
                }
                else
                {
                    staff.Position = null;
                }
                staff.Address = model.Address;
                staff.DateOfBirth = model.DateOfBirth;
                staff.Gender = model.Gender;
                staff.HireDate = model.HireDate;

                // Update Profile
                if (staff.Profile == null)
                {
                    staff.Profile = new StaffProfile { StaffId = staff.Id };
                    _context.StaffProfiles.Add(staff.Profile);
                }
                staff.Profile.IdentityCard = model.IdentityCard;
                staff.Profile.IssueDate = model.IssueDate;
                staff.Profile.IssuePlace = model.IssuePlace;
                staff.Profile.Ethnicity = model.Ethnicity;
                staff.Profile.Religion = model.Religion;
                staff.Profile.MaritalStatus = model.MaritalStatus;

                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật hồ sơ: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult AddPayrollAjax([FromBody] Payroll model)
        {
            try
            {
                if (model.StaffId <= 0 || model.Month <= 0 || model.Year <= 0)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

                var existing = _context.Payrolls.FirstOrDefault(p => p.StaffId == model.StaffId && p.Month == model.Month && p.Year == model.Year);
                if (existing != null)
                {
                    existing.Bonus += model.Bonus;
                    existing.Deduction += model.Deduction;
                    existing.Notes = model.Notes ?? existing.Notes;
                }
                else
                {
                    var staffCode = _context.Staff.Where(s => s.Id == model.StaffId).Select(s => s.StaffCode).FirstOrDefault();
                    model.PayrollCode = $"BL-{staffCode}-{model.Month:D2}{model.Year}";
                    _context.Payrolls.Add(model);
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi thêm bảng lương: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult ToggleActive([FromBody] Staff toggleRequest)
        {
            try
            {
                var existing = _context.Staff.FirstOrDefault(s => s.Id == toggleRequest.Id);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy nhân viên." });
                }

                existing.IsActive = !existing.IsActive;
                existing.Status = existing.IsActive ? "Hoạt động" : "Nghỉ việc";
                
                if (!existing.IsActive)
                {
                    var activeContracts = _context.LaborContracts.Where(c => c.StaffId == existing.Id && c.Status == "Hiệu lực").ToList();
                    foreach (var c in activeContracts)
                    {
                        c.Status = "Hết hiệu lực";
                        c.EndDate = DateTime.Now;
                    }
                }

                _context.SaveChanges();

                return Json(new { success = true, isActive = existing.IsActive });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi thay đổi trạng thái nhân viên: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult SavePositionAjax([FromBody] Position model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.PositionCode))
                {
                    return Json(new { success = false, message = "Mã vị trí và Tên vị trí là bắt buộc." });
                }

                if (model.Id == 0)
                {
                    // Add new
                    if (_context.Positions.Any(p => p.PositionCode == model.PositionCode))
                    {
                        return Json(new { success = false, message = "Mã vị trí đã tồn tại." });
                    }
                    _context.Positions.Add(model);
                }
                else
                {
                    // Edit existing
                    var existing = _context.Positions.FirstOrDefault(p => p.Id == model.Id);
                    if (existing == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy vị trí." });
                    }

                    if (existing.PositionCode != model.PositionCode && _context.Positions.Any(p => p.PositionCode == model.PositionCode))
                    {
                        return Json(new { success = false, message = "Mã vị trí mới đã tồn tại." });
                    }

                    // Sync old position name in staff records if name has changed
                    if (existing.Name != model.Name)
                    {
                        var relatedStaff = _context.Staff.Where(s => s.PositionId == existing.Id).ToList();
                        foreach (var s in relatedStaff)
                        {
                            s.Position = model.Name;
                        }
                    }

                    existing.PositionCode = model.PositionCode;
                    existing.Name = model.Name;
                    existing.Description = model.Description;
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu vị trí: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult DeletePositionAjax(int id)
        {
            try
            {
                var existing = _context.Positions.FirstOrDefault(p => p.Id == id);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy vị trí." });
                }

                var hasStaff = _context.Staff.Any(s => s.PositionId == id);
                if (hasStaff)
                {
                    return Json(new { success = false, message = "Không thể xóa vị trí này vì đang có nhân viên đảm nhiệm." });
                }

                _context.Positions.Remove(existing);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi xóa vị trí: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả vị trí để dropdown
        /// GET: /staff/positions-list
        /// </summary>
        [HttpGet("staff/positions-list")]
        public IActionResult PositionsList()
        {
            try
            {
                var positions = _context.Positions
                    .OrderBy(p => p.PositionCode)
                    .Select(p => new { p.Id, p.PositionCode, p.Name, p.Description })
                    .ToList();

                return Json(new { success = true, data = positions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật vị trí cho nhân viên
        /// POST: /staff/update-position/{staffId}
        /// </summary>
        [HttpPost("staff/update-position")]
        public IActionResult UpdateStaffPositionAjax([FromBody] UpdateStaffPositionRequest request)
        {
            try
            {
                if (request.StaffId <= 0 || request.PositionId <= 0)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                }

                var staff = _context.Staff.Include(s => s.PositionEntity).FirstOrDefault(s => s.Id == request.StaffId);
                if (staff == null)
                {
                    return Json(new { success = false, message = "Nhân viên không tồn tại." });
                }

                var position = _context.Positions.FirstOrDefault(p => p.Id == request.PositionId);
                if (position == null)
                {
                    return Json(new { success = false, message = "Vị trí không tồn tại." });
                }

                var oldPosition = staff.PositionEntity?.Name ?? "Không xác định";
                staff.PositionId = request.PositionId;
                staff.PositionEntity = position;
                staff.Position = position.Name; // Sync legacy field

                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Cập nhật vị trí từ '{oldPosition}' thành '{position.Name}' thành công.",
                    data = new { staffId = staff.Id, positionId = position.Id, positionName = position.Name }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateStaffPosition Error: {ex}");
                return Json(new { success = false, message = $"Lỗi: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        /// <summary>
        /// Reassign positions - gán lại vị trí cho nhân viên từ vị trí cũ sang vị trí mới
        /// POST: /staff/reassign-position
        /// </summary>
        [HttpPost("staff/reassign-position")]
        public IActionResult ReassignPositionAjax([FromBody] ReassignPositionRequest request)
        {
            try
            {
                if (request.FromPositionId <= 0 || request.ToPositionId <= 0)
                {
                    return Json(new { success = false, message = "Dữ liệu vị trí không hợp lệ." });
                }

                var fromPosition = _context.Positions.FirstOrDefault(p => p.Id == request.FromPositionId);
                var toPosition = _context.Positions.FirstOrDefault(p => p.Id == request.ToPositionId);

                if (fromPosition == null || toPosition == null)
                {
                    return Json(new { success = false, message = "Vị trí không tồn tại." });
                }

                var staffList = _context.Staff.Where(s => s.PositionId == request.FromPositionId).ToList();
                if (!staffList.Any())
                {
                    return Json(new { success = false, message = $"Không có nhân viên nào giữ vị trí '{fromPosition.Name}'." });
                }

                foreach (var staff in staffList)
                {
                    staff.PositionId = request.ToPositionId;
                    staff.Position = toPosition.Name; // Sync legacy field
                }

                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Đã gán lại {staffList.Count} nhân viên từ vị trí '{fromPosition.Name}' sang vị trí '{toPosition.Name}'.",
                    data = new { staffCount = staffList.Count }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReassignPosition Error: {ex}");
                return Json(new { success = false, message = $"Lỗi: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        /// <summary>
        /// Xóa toàn bộ dữ liệu và khởi tạo lại (yêu cầu xác nhận)
        /// POST: /staff/reset-all-data
        /// </summary>
        [HttpPost("staff/reset-all-data")]
        public IActionResult ResetAllDataAjax([FromBody] ResetDataRequest request)
        {
            try
            {
                if (!request.Confirmed)
                {
                    return Json(new { success = false, message = "Bạn phải xác nhận hành động này." });
                }

                if (request.ConfirmationCode != "XOA_HAY_DU_LIEU")
                {
                    return Json(new { success = false, message = "Mã xác nhận không chính xác." });
                }

                // Call the clear data method from DbInitializer
                DbInitializer.Initialize(_context, HttpContext.RequestServices, clearOldData: true).GetAwaiter().GetResult();

                return Json(new
                {
                    success = true,
                    message = "Đã xóa sạch dữ liệu cũ và khởi tạo dữ liệu mới thành công!"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ResetAllData Error: {ex}");
                return Json(new { success = false, message = $"Lỗi: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        public class UpdateStaffPositionRequest
        {
            public int StaffId { get; set; }
            public int PositionId { get; set; }
        }

        public class ReassignPositionRequest
        {
            public int FromPositionId { get; set; }
            public int ToPositionId { get; set; }
        }

        public class ResetDataRequest
        {
            public bool Confirmed { get; set; }
            public string ConfirmationCode { get; set; } = string.Empty;
        }
    }
}
