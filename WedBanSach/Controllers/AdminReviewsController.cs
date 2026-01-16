using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminReviewsController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminReviewsController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var query = _context.Reviews
            .Include(r => r.Book)
            .Include(r => r.User)
            .AsQueryable();

        var totalRecords = await query.CountAsync();
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        return View(reviews);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review != null)
        {
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa đánh giá thành công!";
        }
        return RedirectToAction(nameof(Index));
    }
}
