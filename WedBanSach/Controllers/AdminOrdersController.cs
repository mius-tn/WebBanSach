using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminOrdersController : Controller
{
    private readonly BookStoreDbContext _context;
    private readonly WedBanSach.Services.EmailService _emailService;

    public AdminOrdersController(BookStoreDbContext context, WedBanSach.Services.EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index(string status = "", int page = 1, int pageSize = 20)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Book)
            .Include(o => o.Payments)
            .Include(o => o.Shippings)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.OrderStatus == status);
        }

        var totalRecords = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        ViewBag.Statuses = new Dictionary<string, string>
        {
            { "Pending", "Chờ xử lý" },
            { "Confirmed", "Đã xác nhận" },
            { "Shipping", "Đang vận chuyển" },
            { "Completed", "Hoàn tất" },
            { "Cancelled", "Đã hủy" }
        };

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Book)
            .ThenInclude(b => b.BookImages)
            .Include(o => o.Payments)
            .Include(o => o.Shippings)
            .FirstOrDefaultAsync(o => o.OrderID == id);

        if (order == null)
            return NotFound();

        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        // Must Include relations for Email Service
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Book)
            .ThenInclude(b => b.BookImages)
            .FirstOrDefaultAsync(o => o.OrderID == id);

        if (order == null)
            return NotFound();

        string oldStatus = order.OrderStatus ?? "";
        order.OrderStatus = status;

        // Logic: Only update stock/sold when status changes TO "Completed"
        if (status == "Completed" && oldStatus != "Completed")
        {
            var orderDetails = order.OrderDetails; // Already included

            foreach (var detail in orderDetails)
            {
                if (detail.Book != null)
                {
                    detail.Book.StockQuantity -= detail.Quantity;
                    detail.Book.SoldQuantity += detail.Quantity;

                    // Add Inventory Log
                    var log = new Models.InventoryLog
                    {
                        BookID = detail.BookID,
                        ChangeQuantity = -detail.Quantity,
                        Reason = $"Order #{id} Delivered",
                        CreatedAt = DateTime.Now
                    };
                    _context.InventoryLogs.Add(log);
                }
            }
        }
        
        await _context.SaveChangesAsync();
        
        // EMAIL TRIGGER: If Status changed to Confirmed
        if (status == "Confirmed" && oldStatus != "Confirmed")
        {
             await _emailService.SendOrderConfirmationEmailAsync(order);
        }

        // Send Notification to user about status change
        string title = "Cập nhật trạng thái đơn hàng";
        string message = status switch
        {
            "Confirmed" => $"Đơn hàng #{id} của bạn đã được xác nhận.",
            "Shipping" => $"Đơn hàng #{id} của bạn đang được vận chuyển.",
            "Completed" => $"Đơn hàng #{id} của bạn đã được giao thành công. Cảm ơn bạn!",
            "Cancelled" => $"Đơn hàng #{id} của bạn đã bị hủy.",
            _ => $"Trạng thái đơn hàng #{id} đã được thay đổi thành: {status}"
        };

        var notification = new Models.Notification
        {
            UserID = order.UserID,
            Title = title,
            Message = message,
            Type = "Order",
            RedirectUrl = "/Account/Orders",
            CreatedAt = DateTime.Now
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công!";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateShipping(int id, string shippingCompany, string trackingNumber)
    {
        var shipping = await _context.Shippings.FirstOrDefaultAsync(s => s.OrderID == id);
        
        if (shipping == null)
        {
            shipping = new Models.Shipping
            {
                OrderID = id,
                ShippingCompany = shippingCompany,
                TrackingNumber = trackingNumber,
                ShippingStatus = "Shipping"
            };
            _context.Shippings.Add(shipping);
        }
        else
        {
            shipping.ShippingCompany = shippingCompany;
            shipping.TrackingNumber = trackingNumber;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật thông tin vận chuyển thành công!";
        return RedirectToAction(nameof(Details), new { id });
    }
}
