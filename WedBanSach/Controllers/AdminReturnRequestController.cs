using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;
using WedBanSach.Services;
using WedBanSach.ViewModels;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminReturnRequestController : Controller
{
    private readonly IReturnRequestService _returnRequestService;
    private readonly BookStoreDbContext _context;

    public AdminReturnRequestController(IReturnRequestService returnRequestService, BookStoreDbContext context)
    {
        _returnRequestService = returnRequestService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var requests = await _returnRequestService.GetAllRequestsAsync();

        // Calculate dashboard statistics
        var stats = new DashboardStatsViewModel
        {
            TotalRequests = await _context.ReturnRequests.CountAsync(),
            PendingRequests = await _context.ReturnRequests.CountAsync(r => r.Status == "Pending"),
            ApprovedRequests = await _context.ReturnRequests.CountAsync(r => r.Status == "Approved"),
            RejectedRequests = await _context.ReturnRequests.CountAsync(r => r.Status == "Rejected"),
            CompletedRequests = await _context.ReturnRequests.CountAsync(r => r.Status == "Completed"),
            TotalRefunded = await _context.ReturnRequests.Where(r => r.Status == "Completed").SumAsync(r => r.RefundAmount ?? 0),
            
            TotalWarrantyRequests = await _context.WarrantyRequests.CountAsync(),
            PendingWarrantyRequests = await _context.WarrantyRequests.CountAsync(w => w.Status == "Pending")
        };

        // Monthly Stats (Last 6 Months)
        var monthlyData = new List<MonthlyRefundStat>();
        for (int i = 5; i >= 0; i--)
        {
            var date = DateTime.Today.AddMonths(-i);
            var monthLabel = $"T{date.Month}/{date.Year}";
            
            var totalRefundForMonth = await _context.ReturnRequests
                .Where(r => r.Status == "Completed" && r.UpdatedAt.Month == date.Month && r.UpdatedAt.Year == date.Year)
                .SumAsync(r => r.RefundAmount ?? 0);

            var countForMonth = await _context.ReturnRequests
                .Where(r => r.Status == "Completed" && r.UpdatedAt.Month == date.Month && r.UpdatedAt.Year == date.Year)
                .CountAsync();

            stats.MonthlyLabels.Add(monthLabel);
            stats.MonthlyRefundAmounts.Add(totalRefundForMonth);
            stats.MonthlyRequestCounts.Add(countForMonth);
        }

        ViewBag.Stats = stats;
        return View(requests);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var request = await _returnRequestService.GetRequestByIdAsync(id);
        if (request == null) return NotFound();

        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string adminNote)
    {
        if (string.IsNullOrEmpty(status))
        {
            return Json(new { success = false, message = "Trạng thái không hợp lệ." });
        }

        var result = await _returnRequestService.UpdateStatusAsync(id, status, adminNote);
        if (result)
        {
            return Json(new { success = true, message = "Cập nhật trạng thái và thông báo cho khách hàng thành công!" });
        }
        return Json(new { success = false, message = "Cập nhật trạng thái thất bại." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRefund(int id, decimal refundAmount, string method, string adminNote, string? transactionCode)
    {
        if (refundAmount < 0)
        {
            return Json(new { success = false, message = "Số tiền hoàn lại không được âm." });
        }

        var result = await _returnRequestService.ApproveRefundAsync(id, refundAmount, method, adminNote, transactionCode);
        if (result)
        {
            return Json(new { success = true, message = "Duyệt hoàn tiền và gửi email thành công!" });
        }
        return Json(new { success = false, message = "Duyệt hoàn tiền thất bại." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int id, string adminNote)
    {
        if (string.IsNullOrEmpty(adminNote))
        {
            return Json(new { success = false, message = "Vui lòng nhập lý do từ chối yêu cầu." });
        }

        var result = await _returnRequestService.RejectRequestAsync(id, adminNote);
        if (result)
        {
            return Json(new { success = true, message = "Đã từ chối yêu cầu và gửi email giải thích lý do cho khách." });
        }
        return Json(new { success = false, message = "Từ chối yêu cầu thất bại." });
    }
}

public class MonthlyRefundStat
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}
