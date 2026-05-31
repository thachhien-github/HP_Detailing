using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HP_Detailing.Data;
using HP_Detailing.Models;
using HP_Detailing.Models.ViewModels;

namespace HP_Detailing.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly HP_DetailingDbContext _context;

        public SettingsController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, HP_DetailingDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var model = new SettingsViewModel();
            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            model.PaymentMethods = await _context.PaymentMethods.ToListAsync();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                model.Accounts.Add(new UserAccountViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    FullName = u.FullName ?? u.UserName,
                    Email = u.Email,
                    Role = roles.FirstOrDefault() ?? "No Role",
                    IsActive = u.IsActive,
                    AvatarUrl = string.IsNullOrEmpty(u.AvatarUrl) ? "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(u.FullName ?? u.UserName) : u.AvatarUrl
                });
            }

            return View(model);
        }

        [HttpPost("settings/accounts/save")]
        public async Task<IActionResult> SaveAccount([FromBody] CreateOrUpdateUserModel input)
        {
            if (string.IsNullOrWhiteSpace(input.UserName))
                return Json(new { success = false, message = "Tên đăng nhập không hợp lệ" });

            AppUser user;
            bool isNew = string.IsNullOrEmpty(input.Id);

            if (isNew)
            {
                user = new AppUser
                {
                    UserName = input.UserName,
                    Email = input.Email,
                    FullName = input.FullName,
                    IsActive = true,
                    RequirePasswordChange = true // Yêu cầu đổi pass lần đầu
                };
                var createResult = await _userManager.CreateAsync(user, string.IsNullOrEmpty(input.Password) ? "hpdetailing123" : input.Password);
                if (!createResult.Succeeded)
                {
                    return Json(new { success = false, message = string.Join(", ", createResult.Errors.Select(e => e.Description)) });
                }
            }
            else
            {
                user = await _userManager.FindByIdAsync(input.Id);
                if (user == null) return Json(new { success = false, message = "Tài khoản không tồn tại" });
                
                user.FullName = input.FullName;
                user.Email = input.Email;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return Json(new { success = false, message = string.Join(", ", updateResult.Errors.Select(e => e.Description)) });
                }

                if (!string.IsNullOrEmpty(input.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    await _userManager.ResetPasswordAsync(user, token, input.Password);
                }
            }

            // Xử lý Role
            if (!string.IsNullOrEmpty(input.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(input.Role))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, input.Role);
                }
            }

            return Json(new { success = true, message = isNew ? "Thêm tài khoản thành công" : "Cập nhật tài khoản thành công" });
        }

        [HttpPost("settings/accounts/delete/{id}")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng" });

            if (user.UserName == "admin_hpd" || user.UserName == User.Identity.Name)
            {
                return Json(new { success = false, message = "Không thể xóa tài khoản hệ thống này" });
            }

            user.IsActive = false; // Soft delete
            await _userManager.UpdateAsync(user);
            return Json(new { success = true, message = "Đã khóa tài khoản thành công" });
        }

        [HttpPost("settings/accounts/activate/{id}")]
        public async Task<IActionResult> ActivateAccount(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng" });

            user.IsActive = true; 
            await _userManager.UpdateAsync(user);
            return Json(new { success = true, message = "Đã kích hoạt tài khoản thành công" });
        }

        [HttpPost("settings/accounts/hard-delete/{id}")]
        public async Task<IActionResult> HardDeleteAccount(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng" });

            if (user.UserName == "admin_hpd" || user.UserName == User.Identity?.Name)
            {
                return Json(new { success = false, message = "Không thể xóa tài khoản này" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }

            return Json(new { success = true, message = "Đã xóa vĩnh viễn tài khoản" });
        }

        [HttpGet("settings/services")]
        public IActionResult ServicesUi() => RedirectPermanent("/catalog/services");

        [HttpGet("settings/quotas-ui")]
        public IActionResult QuotasUi() => RedirectPermanent("/catalog/quotas");

        // ─── PHƯƠNG THỨC THANH TOÁN ─────────────────────────────────

        [HttpPost("settings/payments/save")]
        public async Task<IActionResult> SavePaymentMethod([FromBody] PaymentMethodInputModel input)
        {
            if (string.IsNullOrWhiteSpace(input.BankShortName))
                return Json(new { success = false, message = "Tên viết tắt không được để trống" });
            if (string.IsNullOrWhiteSpace(input.BankFullName))
                return Json(new { success = false, message = "Tên đầy đủ không được để trống" });

            PaymentMethod method;
            bool isNew = input.Id == 0;

            if (isNew)
            {
                method = new PaymentMethod
                {
                    BankFullName = input.BankFullName,
                    BankShortName = input.BankShortName,
                    AccountNumber = input.AccountNumber,
                    Owner = input.Owner,
                    IsActive = input.IsActive,
                    IsDefault = input.IsDefault
                };
                _context.PaymentMethods.Add(method);
            }
            else
            {
                method = await _context.PaymentMethods.FindAsync(input.Id);
                if (method == null) return Json(new { success = false, message = "Phương thức thanh toán không tồn tại" });

                method.BankFullName = input.BankFullName;
                method.BankShortName = input.BankShortName;
                method.AccountNumber = input.AccountNumber;
                method.Owner = input.Owner;
                method.IsActive = input.IsActive;
                method.IsDefault = input.IsDefault;
            }

            if (method.IsDefault)
            {
                // Tắt các mặc định khác
                var defaults = await _context.PaymentMethods.Where(p => p.IsDefault && p.Id != method.Id).ToListAsync();
                foreach (var d in defaults)
                {
                    d.IsDefault = false;
                }
            }
            else
            {
                // Nếu đây là cái mặc định duy nhất được sửa thành false, ta cần đảm bảo có ít nhất 1 mặc định hoạt động
                var anyOtherDefault = await _context.PaymentMethods.AnyAsync(p => p.IsDefault && p.Id != method.Id && p.IsActive);
                if (!anyOtherDefault && method.IsActive)
                {
                    method.IsDefault = true; // Ép buộc giữ lại mặc định
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = isNew ? "Thêm phương thức thành công" : "Cập nhật phương thức thành công" });
        }

        [HttpPost("settings/payments/delete/{id}")]
        public async Task<IActionResult> DeletePaymentMethod(int id)
        {
            var method = await _context.PaymentMethods.FindAsync(id);
            if (method == null) return Json(new { success = false, message = "Không tìm thấy phương thức thanh toán" });

            if (method.IsDefault)
            {
                return Json(new { success = false, message = "Không thể xóa phương thức thanh toán mặc định" });
            }

            var hasInvoices = await _context.Invoices.AnyAsync(i => i.PaymentMethodId == id);
            if (hasInvoices)
            {
                method.IsActive = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã khóa phương thức thanh toán này (do đã có hóa đơn liên kết)" });
            }
            else
            {
                _context.PaymentMethods.Remove(method);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa phương thức thanh toán thành công" });
            }
        }

        [HttpPost("settings/payments/toggle-active/{id}")]
        public async Task<IActionResult> TogglePaymentMethodActive(int id)
        {
            var method = await _context.PaymentMethods.FindAsync(id);
            if (method == null) return Json(new { success = false, message = "Không tìm thấy phương thức thanh toán" });

            if (method.IsDefault && method.IsActive)
            {
                return Json(new { success = false, message = "Không thể khóa phương thức thanh toán mặc định" });
            }

            method.IsActive = !method.IsActive;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = method.IsActive ? "Đã kích hoạt phương thức thanh toán" : "Đã khóa phương thức thanh toán" });
        }

        [HttpPost("settings/payments/set-default/{id}")]
        public async Task<IActionResult> SetDefaultPaymentMethod(int id)
        {
            var method = await _context.PaymentMethods.FindAsync(id);
            if (method == null) return Json(new { success = false, message = "Không tìm thấy phương thức thanh toán" });

            if (!method.IsActive)
            {
                return Json(new { success = false, message = "Không thể đặt phương thức đang bị khóa làm mặc định" });
            }

            method.IsDefault = true;

            var otherDefaults = await _context.PaymentMethods.Where(p => p.IsDefault && p.Id != id).ToListAsync();
            foreach (var od in otherDefaults)
            {
                od.IsDefault = false;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã đặt làm mặc định thành công" });
        }
    }

    public class PaymentMethodInputModel
    {
        public int Id { get; set; }
        public string BankFullName { get; set; }
        public string BankShortName { get; set; }
        public string? AccountNumber { get; set; }
        public string? Owner { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
