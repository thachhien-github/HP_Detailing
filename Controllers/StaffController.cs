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

            return View(staffList);
        }

        [HttpGet]
        public IActionResult Detail(int id)
        {
            var staff = _context.Staff
                .Include(s => s.Profile)
                .Include(s => s.LaborContracts)
                .Include(s => s.Payrolls)
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

            return View(staff);
        }

        public class CreateStaffRequest
        {
            public string StaffCode { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Position { get; set; }
            public string? Specialty { get; set; }
            public bool Gender { get; set; }
            public string? Address { get; set; }
            public DateTime? HireDate { get; set; }
            public decimal BasicSalary { get; set; }
        }

        [HttpPost]
        public IActionResult CreateAjax([FromBody] CreateStaffRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.StaffCode) || string.IsNullOrWhiteSpace(model.FullName))
            {
                return Json(new { success = false, message = "Mã NV và Họ Tên là bắt buộc." });
            }

            if (_context.Staff.Any(s => s.StaffCode == model.StaffCode))
            {
                return Json(new { success = false, message = "Mã nhân viên đã tồn tại." });
            }

            var newStaff = new Staff
            {
                StaffCode = model.StaffCode,
                FullName = model.FullName,
                Phone = model.Phone,
                Gender = model.Gender,
                Address = model.Address,
                HireDate = model.HireDate ?? DateTime.Now,
                Position = model.Position,
                Specialty = model.Specialty,
                IsActive = true,
                Status = "Hoạt động"
            };

            _context.Staff.Add(newStaff);
            _context.SaveChanges(); 

            var profile = new StaffProfile { StaffId = newStaff.Id };
            _context.StaffProfiles.Add(profile);

            var contract = new LaborContract
            {
                ContractCode = $"HDLD-{newStaff.StaffCode}-{DateTime.Now:yyMM}",
                StaffId = newStaff.Id,
                StartDate = newStaff.HireDate,
                BasicSalary = model.BasicSalary > 0 ? model.BasicSalary : 0,
                ContractType = "Thử việc / Toàn thời gian",
                Status = "Hiệu lực"
            };
            _context.LaborContracts.Add(contract);

            _context.SaveChanges();

            return Json(new { success = true, data = newStaff });
        }

        public class UpdateProfileRequest
        {
            public int StaffId { get; set; }
            public string StaffCode { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Position { get; set; }
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
            staff.Position = model.Position;
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

        [HttpPost]
        public IActionResult AddPayrollAjax([FromBody] Payroll model)
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

        [HttpPost]
        public IActionResult ToggleActive([FromBody] Staff toggleRequest)
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
    }
}
