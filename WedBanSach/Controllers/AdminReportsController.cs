using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminReportsController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminReportsController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string period = "month")
    {
        var now = DateTime.Now;
        DateTime startDate;

        switch (period)
        {
            case "week":
                startDate = now.AddDays(-7);
                break;
            case "month":
                startDate = new DateTime(now.Year, now.Month, 1);
                break;
            case "year":
                startDate = new DateTime(now.Year, 1, 1);
                break;
            default:
                startDate = new DateTime(now.Year, now.Month, 1);
                break;
        }

        // Revenue by day
        var revenueByDay = await _context.Orders
            .Where(o => o.OrderStatus == "Completed" && o.OrderDate >= startDate)
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
            .Include(od => od.Order)
            .Where(od => od.Order.OrderStatus == "Completed" && od.Order.OrderDate >= startDate)
            .GroupBy(od => od.Book)
            .Select(g => new
            {
                Book = g.Key,
                TotalSold = g.Sum(od => od.Quantity),
                Revenue = g.Sum(od => od.Quantity * od.UnitPrice)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(10)
            .ToListAsync();

        // Top customers
        var topCustomers = await _context.Orders
            .Include(o => o.User)
            .Where(o => o.OrderStatus == "Completed" && o.OrderDate >= startDate)
            .GroupBy(o => o.User)
            .Select(g => new
            {
                User = g.Key,
                OrderCount = g.Count(),
                TotalSpent = g.Sum(o => o.TotalAmount ?? 0)
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(10)
            .ToListAsync();

        ViewBag.Period = period;
        ViewBag.RevenueByDay = revenueByDay;
        ViewBag.TopBooks = topBooks;
        ViewBag.TopCustomers = topCustomers;

        return View();
    }
}
