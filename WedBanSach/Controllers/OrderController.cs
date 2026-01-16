using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers
{
    public class OrderController : Controller
    {
        private readonly BookStoreDbContext _context;

        public OrderController(BookStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> History(string status = "all", int page = 1)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);
            var query = _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Book).ThenInclude(b => b.BookImages)
                .Where(o => o.UserID == userId);

            // Filtering logic
            switch (status)
            {
                case "pending":
                    query = query.Where(o => o.OrderStatus == "Pending" || o.OrderStatus == "Chờ xử lý");
                    break;
                case "shipping":
                    query = query.Where(o => o.OrderStatus == "Shipping" || o.OrderStatus == "Đang giao");
                    break;
                case "completed":
                    query = query.Where(o => o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn tất");
                    break;
                case "cancelled":
                    query = query.Where(o => o.OrderStatus == "Cancelled" || o.OrderStatus == "Đã hủy");
                    break;
                // "review" filter is tricky in SQL directly without joining Reviews. 
                // For now, we returns 'completed' for 'review' tab and filter in memory or separate query if needed.
                // Or simplified: Review tab shows Completed orders, user filters visually?
                // The User asked for a specific column/tab.
                // Let's implement robust "Review" filter later if needed, for now alias to Completed or handle in View?
                // No, Controller filter is best.
                // To do it properly: Orders where ANY detail is NOT reviewed.
                // query = query.Where(o => (o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn tất") && o.OrderDetails.Any(od => !od.Book.Reviews.Any(r => r.UserID == userId))); 
                // That's complex EF. Let's start with basic statuses.
                // Use "completed" for now and I will filter "unreviewed" in memory if status == "review".
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            if (status == "review")
            {
                // In-memory filter for unreviewed items
                // Only keep orders that are completed AND have at least one book not reviewed by this user
                // We need to fetch user reviews to know this.
                var userReviews = await _context.Reviews.Where(r => r.UserID == userId).Select(r => r.BookID).ToListAsync();
                orders = orders.Where(o => (o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn tất") 
                                           && o.OrderDetails.Any(od => !userReviews.Contains(od.BookID)))
                               .ToList();
            }

            // Pagination Logic
            int pageSize = 5;
            int totalItems = orders.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            // Ensure valid page
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var pagedOrders = orders.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedOrders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                    .ThenInclude(b => b.BookImages)
                .Include(o => o.Payments)
                .Include(o => o.Shippings)
                .FirstOrDefaultAsync(m => m.OrderID == id && m.UserID == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
