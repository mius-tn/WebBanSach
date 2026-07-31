using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;
using WedBanSach.Services;
using WedBanSach.ViewModels;
using System.IO;

namespace WedBanSach.Controllers;

public class ReturnRequestController : Controller
{
    private readonly IReturnRequestService _returnRequestService;
    private readonly IWarrantyRequestService _warrantyRequestService;
    private readonly BookStoreDbContext _dbContext;

    public ReturnRequestController(
        IReturnRequestService returnRequestService,
        IWarrantyRequestService warrantyRequestService,
        BookStoreDbContext dbContext)
    {
        _returnRequestService = returnRequestService;
        _warrantyRequestService = warrantyRequestService;
        _dbContext = dbContext;
    }

    private int? GetCurrentUserId()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (int.TryParse(userIdStr, out var id))
        {
            return id;
        }
        return null;
    }

    public async Task<IActionResult> History()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            TempData["Error"] = "Vui lòng đăng nhập để xem lịch sử yêu cầu đổi trả.";
            return RedirectToAction("Login", "Account");
        }

        var returnRequests = await _returnRequestService.GetRequestsByCustomerIdAsync(userId.Value);
        var warrantyRequests = await _warrantyRequestService.GetRequestsByCustomerIdAsync(userId.Value);

        ViewBag.ReturnRequests = returnRequests;
        ViewBag.WarrantyRequests = warrantyRequests;

        return View();
    }

    public async Task<IActionResult> Detail(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var request = await _returnRequestService.GetRequestByIdAsync(id);
        if (request == null || request.CustomerId != userId.Value)
        {
            return NotFound("Không tìm thấy yêu cầu đổi trả này hoặc bạn không có quyền xem.");
        }

        return View(request);
    }

    public async Task<IActionResult> Create(int? orderId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            TempData["Error"] = "Vui lòng đăng nhập để gửi yêu cầu hỗ trợ.";
            return RedirectToAction("Login", "Account");
        }

        if (!orderId.HasValue || orderId.Value == 0)
        {
            TempData["Error"] = "Vui lòng chọn đơn hàng cần đổi trả/bảo hành từ lịch sử mua hàng của bạn.";
            return Redirect("/lich-su-don-hang");
        }

        // Verify order ownership
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.OrderID == orderId.Value && o.UserID == userId.Value);

        if (order == null)
        {
            TempData["Error"] = "Đơn hàng không hợp lệ hoặc không thuộc tài khoản của bạn.";
            return Redirect("/lich-su-don-hang");
        }

        // Load all items in this order with book covers, titles, and quantities bought
        var orderProducts = await _dbContext.OrderDetails
            .Include(od => od.Book)
                .ThenInclude(b => b.BookImages)
            .Where(od => od.OrderID == orderId.Value)
            .Select(od => new
            {
                BookID = od.Book.BookID,
                Title = od.Book.Title,
                Price = od.UnitPrice ?? od.Book.OriginalPrice,
                QuantityBought = od.Quantity,
                ImageUrl = od.Book.BookImages.FirstOrDefault(bi => bi.IsMain).ImageUrl 
                           ?? od.Book.BookImages.FirstOrDefault().ImageUrl 
                           ?? "/images/default-book.png"
            })
            .Distinct()
            .ToListAsync();

        ViewBag.OrderId = orderId.Value;
        ViewBag.OrderProducts = orderProducts;

        return View(new ReturnRequestViewModel { OrderId = orderId.Value });
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderProductsJson(int orderId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var order = await _dbContext.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                    .ThenInclude(b => b.BookImages)
            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId.Value);

        if (order == null)
        {
            return Json(new List<object>());
        }

        var products = order.OrderDetails.Select(od => new
        {
            id = od.Book.BookID,
            title = od.Book.Title,
            price = od.UnitPrice ?? od.Book.OriginalPrice,
            imageUrl = od.Book.BookImages.FirstOrDefault(bi => bi.IsMain)?.ImageUrl 
                       ?? od.Book.BookImages.FirstOrDefault()?.ImageUrl 
                       ?? "/images/default-book.png"
        }).Distinct().ToList();

        return Json(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ReturnRequestViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
        }

        if (!model.AcceptTerms)
        {
            return Json(new { success = false, message = "Bạn cần đồng ý với các điều khoản đổi trả trước khi gửi yêu cầu." });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Dữ liệu nhập vào không hợp lệ.", errors });
        }

        // Verify order ownership
        var isOwnOrder = await _returnRequestService.VerifyOrderOwnershipAsync(model.OrderId, userId.Value);
        if (!isOwnOrder)
        {
            return Json(new { success = false, message = "Mã đơn hàng không hợp lệ hoặc không thuộc tài khoản của bạn." });
        }

        // Upload images
        var uploadedUrls = new List<string>();
        if (model.ProductImages != null && model.ProductImages.Any())
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "return_requests");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            foreach (var file in model.ProductImages)
            {
                if (file.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Tệp ảnh tải lên không được vượt quá 5MB." });
                }

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    return Json(new { success = false, message = "Chỉ chấp nhận các định dạng tệp ảnh .jpg, .jpeg, .png, .webp" });
                }

                var fileName = $"req_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                uploadedUrls.Add($"/uploads/return_requests/{fileName}");
            }
        }

        // Create the request
        var request = new ReturnRequest
        {
            OrderId = model.OrderId,
            CustomerId = userId.Value,
            BookID = model.BookID,
            Quantity = model.Quantity,
            RequestType = model.RequestType,
            Reason = model.Reason,
            Description = model.Description
        };

        var result = await _returnRequestService.CreateRequestAsync(request, uploadedUrls);

        if (result)
        {
            return Json(new { success = true, message = "Gửi yêu cầu đổi trả thành công! Đội ngũ CSKH sẽ kiểm duyệt sớm.", requestId = request.Id });
        }

        return Json(new { success = false, message = "Gửi yêu cầu thất bại. Vui lòng kiểm tra lại thông tin đơn hàng." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitWarranty(int bookId, string issueDescription)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
        }

        if (string.IsNullOrEmpty(issueDescription))
        {
            return Json(new { success = false, message = "Vui lòng nhập mô tả lỗi kỹ thuật." });
        }

        var request = new WarrantyRequest
        {
            ProductId = bookId,
            CustomerId = userId.Value,
            IssueDescription = issueDescription
        };

        var result = await _warrantyRequestService.CreateRequestAsync(request);

        if (result)
        {
            return Json(new { success = true, message = "Đăng ký bảo hành thành công!", requestId = request.Id });
        }

        return Json(new { success = false, message = "Đăng ký bảo hành thất bại." });
    }
}
