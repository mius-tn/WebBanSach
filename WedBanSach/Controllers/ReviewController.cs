using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers
{
    public class ReviewController : Controller
    {
        private readonly BookStoreDbContext _context;

        public ReviewController(BookStoreDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview(int bookId, int rating, string comment, IFormFile? reviewImage)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) 
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để đánh giá." });
            }

            int userId = int.Parse(userIdStr);

            if (rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Vui lòng chọn số sao hợp lệ (1-5)." });
            }

            // Verify purchase
            var hasPurchased = await _context.OrderDetails
                .Include(od => od.Order)
                .AnyAsync(od => od.BookID == bookId && od.Order.UserID == userId && 
                               (od.Order.OrderStatus == "Completed" || od.Order.OrderStatus == "Hoàn tất"));

            if (!hasPurchased)
            {
                // Double check logic: This prevents hacking APIs to review unbought items
                return Json(new { success = false, message = "Bạn chỉ có thể đánh giá sách đã mua thành công." });
            }

            // Handle Image Upload
            string? imageUrl = null;
            if (reviewImage != null && reviewImage.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(reviewImage.FileName);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "reviews");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                
                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await reviewImage.CopyToAsync(stream);
                }
                imageUrl = "/images/reviews/" + fileName;
            }

            var review = new Review
            {
                BookID = bookId,
                UserID = userId,
                Rating = rating,
                Comment = comment,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cảm ơn bạn đã đánh giá!" });
        }
    }
}
