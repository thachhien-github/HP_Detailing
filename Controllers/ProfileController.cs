using HP_Detailing.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace HP_Detailing.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ProfileController(UserManager<AppUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
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
        public async Task<IActionResult> UpdateProfile(string FullName, string PhoneNumber, IFormFile? AvatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FullName = FullName;
            user.PhoneNumber = PhoneNumber;

            // Handle avatar file upload
            if (AvatarFile != null && AvatarFile.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!Array.Exists(allowedTypes, t => t == AvatarFile.ContentType.ToLower()))
                {
                    TempData["ToastMessage"] = "Chỉ chấp nhận file ảnh (JPG, PNG, GIF, WEBP)!";
                    TempData["ToastType"] = "error";
                    return RedirectToAction(nameof(Index));
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "images", "upload");
                Directory.CreateDirectory(uploadFolder);

                var ext = Path.GetExtension(AvatarFile.FileName);
                var fileName = $"avatar_{user.Id}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarFile.CopyToAsync(stream);
                }

                user.AvatarUrl = $"/images/upload/{fileName}";
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

