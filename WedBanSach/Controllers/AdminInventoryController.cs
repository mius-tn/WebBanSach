using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminInventoryController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminInventoryController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 20)
    {
        var query = _context.Books
            .Include(b => b.Publisher)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(b => b.Title.Contains(searchTerm) || 
                                   (b.ISBN != null && b.ISBN.Contains(searchTerm)));
        }

        var totalRecords = await query.CountAsync();
        var books = await query
            .OrderBy(b => b.StockQuantity)
            .ThenBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        return View(books);
    }

    [HttpGet]
    public async Task<IActionResult> Logs(int bookId)
    {
        var logs = await _context.InventoryLogs
            .Include(l => l.Book)
            .Where(l => l.BookID == bookId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(50)
            .ToListAsync();

        ViewBag.BookId = bookId;
        return View(logs);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStock(int bookId, int quantity, string reason)
    {
        var book = await _context.Books.FindAsync(bookId);
        if (book == null)
            return NotFound();

        var changeQuantity = quantity - book.StockQuantity;
        book.StockQuantity = quantity;

        _context.InventoryLogs.Add(new Models.InventoryLog
        {
            BookID = bookId,
            ChangeQuantity = changeQuantity,
            Reason = reason ?? "Manual Update",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật tồn kho thành công!";
        return RedirectToAction(nameof(Index));
    }
}
