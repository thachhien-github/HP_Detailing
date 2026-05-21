using Microsoft.AspNetCore.SignalR;
using HP_Detailing.Models;
using HP_Detailing.Hubs;
using HP_Detailing.Data;
using System;
using System.Threading.Tasks;

namespace HP_Detailing.Extensions
{
    public static class NotificationHelper
    {
        public static async Task SendNotificationAsync(
            HP_DetailingDbContext context,
            IHubContext<NotificationHub> hubContext,
            string title,
            string message,
            string? type = null,
            string? actionUrl = null,
            string? targetUserId = null)
        {
            try
            {
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    ActionUrl = actionUrl,
                    TargetUserId = targetUserId
                };
                context.Notifications.Add(notification);
                await context.SaveChangesAsync();

                // Gửi realtime qua SignalR Hub cho tất cả clients đang kết nối
                await hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    type = notification.Type,
                    isRead = notification.IsRead,
                    createdAt = notification.CreatedAt,
                    actionUrl = notification.ActionUrl,
                    targetUserId = notification.TargetUserId
                });
            }
            catch (Exception)
            {
                // Fallback nếu có lỗi DB hoặc SignalR để tránh crash luồng chính
            }
        }
    }
}
