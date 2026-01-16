using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;
using WedBanSach.ViewModels;

namespace WedBanSach.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly BookStoreDbContext _context;

    public HomeController(ILogger<HomeController> logger, BookStoreDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [Route("")]
    //[Route("TrangChu")]
    public async Task<IActionResult> Index(int? categoryId)
    {
        // Cleanup Abandoned Orders
        var pendingOrderIdStr = HttpContext.Session.GetString("PendingOrderId");
        if (!string.IsNullOrEmpty(pendingOrderIdStr) && int.TryParse(pendingOrderIdStr, out int pendingId))
        {
            var pendingOrder = await _context.Orders.FindAsync(pendingId);
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderID == pendingId);
            if (pendingOrder != null && (payment == null || payment.PaymentStatus != "Paid"))
            {
                // Delete Related Data first (Fix FK Constraints)
                var relatedDetails = _context.OrderDetails.Where(od => od.OrderID == pendingId);
                _context.OrderDetails.RemoveRange(relatedDetails);
                
                var relatedShippings = _context.Shippings.Where(s => s.OrderID == pendingId);
                _context.Shippings.RemoveRange(relatedShippings);

                var relatedPayments = _context.Payments.Where(p => p.OrderID == pendingId);
                _context.Payments.RemoveRange(relatedPayments);

                var relatedNotifications = _context.Notifications.Where(n => n.Message.Contains($"#{pendingId}"));
                _context.Notifications.RemoveRange(relatedNotifications);

                _context.Orders.Remove(pendingOrder);
                await _context.SaveChangesAsync();
            }
            HttpContext.Session.Remove("PendingOrderId");
        }

        var booksQuery = _context.Books
            .Include(b => b.BookImages)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Where(b => b.Status == "Active");

        var featuredBooks = await booksQuery
            .OrderByDescending(b => b.DiscountPrice.HasValue ? (b.Price - b.DiscountPrice.Value) : 0) // prioritize big discounts
            .Take(12)
            .ToListAsync();

        var newBooks = await booksQuery
            .OrderByDescending(b => b.CreatedAt)
            .Take(12)
            .ToListAsync();

        var categories = await _context.Categories.ToListAsync();

        var viewModel = new HomeViewModel
        {
            Categories = categories,
            FeaturedBooks = featuredBooks,
            NewBooks = newBooks
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
