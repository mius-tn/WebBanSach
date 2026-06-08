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

        [Route("lich-su-don-hang")]
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
                case "my-reviews":
                    var reviews = await _context.Reviews
                        .Include(r => r.Book)
                            .ThenInclude(b => b.BookImages)
                        .Where(r => r.UserID == userId)
                        .OrderByDescending(r => r.CreatedAt)
                        .ToListAsync();
                    ViewBag.UserReviews = reviews;
                    // Return no orders for this tab, as we display reviews from ViewBag
                    query = query.Where(o => false); 
                    break;
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            if (status == "review")
            {
                // In-memory filter for unreviewed items
                // Only keep orders that are completed AND have at least one book not reviewed by this user
                var userReviewBookIds = await _context.Reviews.Where(r => r.UserID == userId).Select(r => r.BookID).ToListAsync();
                orders = orders.Where(o => (o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn tất") 
                                           && o.OrderDetails.Any(od => !userReviewBookIds.Contains(od.BookID)))
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

        [Route("chi-tiet-don-hang/{id}")]
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


        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return Json(new { success = false, message = "Bạn cần đăng nhập." });

            int userId = int.Parse(userIdStr);
            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId);

            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            if (order.OrderStatus != "Pending" && order.OrderStatus != "Chờ xử lý")
            {
                return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng đang chờ xử lý." });
            }

            order.OrderStatus = "Cancelled";

            // Also update associated payment statuses to "Failed" (which represents Canceled/Hủy)
            if (order.Payments != null)
            {
                foreach (var payment in order.Payments)
                {
                    payment.PaymentStatus = "Failed";
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã hủy đơn hàng thành công." });
        }
    }
}
