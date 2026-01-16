using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;
using System.Linq;
using System.Threading.Tasks;

namespace WedBanSach.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly BookStoreDbContext _context;

        public NotificationBellViewComponent(BookStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Content(""); // Don't show if not logged in
            }

            int userId = int.Parse(userIdStr);

            var notifications = await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserID == userId && !n.IsRead);

            ViewBag.UnreadCount = unreadCount;

            return View(notifications);
        }
    }
}
