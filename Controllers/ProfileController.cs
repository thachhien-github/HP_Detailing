using HP_Detailing.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HP_Detailing.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public ProfileController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(bool mustChangePassword = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (mustChangePassword)
            {
                ViewData["MustChangePassword"] = true;
                ViewData["ToastMessage"] = "Vui lòng đổi mật khẩu mặc định để tiếp tục sử dụng hệ thống!";
                ViewData["ToastType"] = "warning";
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string FullName, string PhoneNumber, string AvatarUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FullName = FullName;
            user.PhoneNumber = PhoneNumber;
            if (!string.IsNullOrEmpty(AvatarUrl))
            {
                user.AvatarUrl = AvatarUrl;
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["ToastMessage"] = "Thông tin đã được cập nhật thành công!";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Có lỗi xảy ra khi cập nhật!";
                TempData["ToastType"] = "error";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword)
            {
                TempData["ToastMessage"] = "Mật khẩu xác nhận không khớp!";
                TempData["ToastType"] = "error";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);

            if (result.Succeeded)
            {
                if (user.RequirePasswordChange)
                {
                    user.RequirePasswordChange = false;
                    await _userManager.UpdateAsync(user);
                }

                TempData["ToastMessage"] = "Đổi mật khẩu thành công!";
                TempData["ToastType"] = "success";
            }
            else
            {
                var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["ToastMessage"] = $"Đổi mật khẩu thất bại: {errorMsg}";
                TempData["ToastType"] = "error";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

