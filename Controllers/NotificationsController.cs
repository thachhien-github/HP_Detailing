using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HP_Detailing.Data;
using HP_Detailing.Models;
using System.Linq;
using System.Threading.Tasks;

namespace HP_Detailing.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly HP_DetailingDbContext _context;

        public NotificationsController(HP_DetailingDbContext context)
        {
            _context = context;
        }

        [HttpGet("Notifications/List")]
        public async Task<IActionResult> List()
        {
            // Lấy 20 thông báo mới nhất
            var list = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();

            var unreadCount = await _context.Notifications.CountAsync(n => !n.IsRead);

            return Json(new { success = true, items = list, unreadCount = unreadCount });
        }

        [HttpPost("Notifications/MarkAsRead")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpPost("Notifications/MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var unread = await _context.Notifications.Where(n => !n.IsRead).ToListAsync();
            foreach (var n in unread)
            {
                n.IsRead = true;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
