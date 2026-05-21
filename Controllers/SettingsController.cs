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

        public SettingsController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var model = new SettingsViewModel();
            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

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
    }
}
