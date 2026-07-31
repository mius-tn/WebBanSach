using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

public class BookReportViewModel
{
    public Book Book { get; set; } = null!;
    public int TotalSold { get; set; }
    public decimal Revenue { get; set; }
    public decimal AvgImportPrice { get; set; }
    public decimal TotalImportCost { get; set; }
    public decimal ProfitOrLoss { get; set; }
}

[AuthorizeAdmin]
public class AdminReportsController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminReportsController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string period = "month", DateTime? fromDate = null, DateTime? toDate = null)
    {
        var now = DateTime.Now;
        DateTime startDate;
        DateTime endDate = now;

        if (period == "custom")
        {
            if (!fromDate.HasValue)
                fromDate = new DateTime(now.Year, now.Month, 1);
            if (!toDate.HasValue)
                toDate = now.Date;

            startDate = fromDate.Value.Date;
            endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
        }
        else
        {
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
                    period = "month";
                    break;
            }
            fromDate = startDate;
            toDate = endDate;
        }

        // 1. Fetch completed order details for the period first to compute correct discounted prices
        var completedOrderDetails = await _context.OrderDetails
            .Include(od => od.Book)
            .Include(od => od.Order)
            .Where(od => od.Order.OrderStatus == "Completed" && od.Order.OrderDate >= startDate && od.Order.OrderDate <= endDate)
            .ToListAsync();

        // Calculate pre-discount subtotal for each order to apply coupon discount proportionally
        var orderSubTotals = completedOrderDetails
            .GroupBy(od => od.OrderID)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(od => od.Quantity * (od.UnitPrice ?? 0))
            );

        // Group by Book to get sales summary
        var booksRaw = completedOrderDetails
            .GroupBy(od => od.Book)
            .Select(g => {
                decimal revenue = g.Sum(od => {
                    decimal subTotal = orderSubTotals[od.OrderID];
                    decimal bookTotal = (od.Order.TotalAmount ?? 0) - (od.Order.ShippingFee ?? 0);
                    decimal ratio = subTotal > 0 ? bookTotal / subTotal : 1m;
                    return od.Quantity * (od.UnitPrice ?? 0) * ratio;
                });
                return new
                {
                    Book = g.Key,
                    TotalSold = g.Sum(od => od.Quantity),
                    Revenue = revenue
                };
            })
            .OrderByDescending(x => x.TotalSold)
            .ToList();

        var bookIds = booksRaw.Select(tb => tb.Book.BookID).ToList();

        // Fetch average import prices for these books
        var importRates = await _context.GoodsReceiptDetails
            .Where(grd => bookIds.Contains(grd.BookID))
            .GroupBy(grd => grd.BookID)
            .Select(g => new
            {
                BookID = g.Key,
                TotalQty = g.Sum(x => x.Quantity),
                TotalCost = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .ToDictionaryAsync(x => x.BookID, x => x.TotalQty > 0 ? x.TotalCost / x.TotalQty : 0);

        var bookRevenues = booksRaw.Select(tb => {
            decimal avgImportPrice = 0;
            if (!importRates.TryGetValue(tb.Book.BookID, out avgImportPrice) || avgImportPrice <= 0)
            {
                // Fallback to 60% of original price if no import receipts are recorded
                avgImportPrice = tb.Book.OriginalPrice * 0.6m;
            }
            decimal totalImportCost = tb.TotalSold * avgImportPrice;
            decimal profitOrLoss = tb.Revenue - totalImportCost;

            return new BookReportViewModel
            {
                Book = tb.Book,
                TotalSold = tb.TotalSold,
                Revenue = tb.Revenue,
                AvgImportPrice = avgImportPrice,
                TotalImportCost = totalImportCost,
                ProfitOrLoss = profitOrLoss
            };
        }).ToList();

        // 2. Top customers
        var topCustomers = await _context.Orders
            .Include(o => o.User)
            .Where(o => o.OrderStatus == "Completed" && o.OrderDate >= startDate && o.OrderDate <= endDate)
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

        // 3. Calculate total period revenue, cost and profit for summary cards
        var allSoldBookIds = completedOrderDetails.Select(od => od.BookID).Distinct().ToList();
        var allImportRates = await _context.GoodsReceiptDetails
            .Where(grd => allSoldBookIds.Contains(grd.BookID))
            .GroupBy(grd => grd.BookID)
            .Select(g => new
            {
                BookID = g.Key,
                TotalQty = g.Sum(x => x.Quantity),
                TotalCost = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .ToDictionaryAsync(x => x.BookID, x => x.TotalQty > 0 ? x.TotalCost / x.TotalQty : 0);

        decimal totalPeriodRevenue = 0;
        decimal totalPeriodCost = 0;

        foreach (var od in completedOrderDetails)
        {
            decimal subTotal = orderSubTotals[od.OrderID];
            decimal bookTotal = (od.Order.TotalAmount ?? 0) - (od.Order.ShippingFee ?? 0);
            decimal ratio = subTotal > 0 ? bookTotal / subTotal : 1m;

            totalPeriodRevenue += od.Quantity * (od.UnitPrice ?? 0) * ratio;

            decimal avgImportPrice = 0;
            if (!allImportRates.TryGetValue(od.BookID, out avgImportPrice) || avgImportPrice <= 0)
            {
                avgImportPrice = od.Book.OriginalPrice * 0.6m;
            }
            totalPeriodCost += od.Quantity * avgImportPrice;
        }

        decimal totalPeriodProfit = totalPeriodRevenue - totalPeriodCost;

        ViewBag.Period = period;
        ViewBag.BookRevenues = bookRevenues;
        ViewBag.TopCustomers = topCustomers;
        ViewBag.TotalPeriodRevenue = totalPeriodRevenue;
        ViewBag.TotalPeriodCost = totalPeriodCost;
        ViewBag.TotalPeriodProfit = totalPeriodProfit;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        return View();
    }
}
