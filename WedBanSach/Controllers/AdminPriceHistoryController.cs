using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Constants;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminPriceHistoryController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminPriceHistoryController(BookStoreDbContext context)
    {
        _context = context;
    }

    [Permission(SystemPermissions.Module_Product, SystemPermissions.Action_View)]
    public async Task<IActionResult> Index(int? bookId)
    {
        var query = _context.PriceHistories.Include(p => p.Book).AsQueryable();
        
        if (bookId.HasValue)
        {
            query = query.Where(p => p.BookID == bookId.Value);
            ViewBag.SelectedBookId = bookId.Value;
        }

        var histories = await query.OrderByDescending(p => p.ChangedAt).ToListAsync();
        
        ViewBag.Books = await _context.Books.Where(b => b.Status == "Active").ToListAsync();
        return View(histories);
    }
}
