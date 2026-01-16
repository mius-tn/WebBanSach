using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Helpers;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var stats = new
        {
            TotalBooks = await _context.Books.CountAsync(),
            TodayOrders = await _context.Orders
                .Where(o => o.OrderDate.Date == today)
                .CountAsync(),
            TotalRevenue = await _context.Orders
                .Where(o => o.OrderStatus == "Completed")
                .SumAsync(o => o.TotalAmount ?? 0),
            TotalUsers = await _context.Users.CountAsync(),
            LowStockBooks = await _context.Books
                .Where(b => b.StockQuantity < 10 && b.Status == "Active")
                .CountAsync()
        };

        // Revenue by day (last 30 days)
        var revenueData = await _context.Orders
            .Where(o => o.OrderStatus == "Completed" && o.OrderDate >= DateTime.Now.AddDays(-30))
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount ?? 0)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Top selling books
        var topBooks = await _context.OrderDetails
            .Include(od => od.Book)
            .GroupBy(od => od.Book)
            .Select(g => new
            {
                Book = g.Key,
                TotalSold = g.Sum(od => od.Quantity)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(10)
            .ToListAsync();

        ViewBag.RevenueData = revenueData;
        ViewBag.TopBooks = topBooks;
        ViewBag.Stats = stats;

        return View();
    }
}
