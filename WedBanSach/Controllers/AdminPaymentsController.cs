using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

[   AuthorizeAdmin]
public class AdminPaymentsController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminPaymentsController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string status = "", int page = 1, int pageSize = 20)
    {
        var query = _context.Payments
            .Include(p => p.Order)
            .ThenInclude(o => o.User)
            .AsQueryable();

        // Calculate Stats
        ViewBag.TotalRevenue = await _context.Payments
            .Where(p => p.PaymentStatus == "Paid")
            .SumAsync(p => p.Amount ?? 0);
        
        ViewBag.PendingRevenue = await _context.Payments
            .Where(p => p.PaymentStatus == "Pending")
            .SumAsync(p => p.Amount ?? 0);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.PaymentStatus == status);
        }

        var totalRecords = await query.CountAsync();
        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        ViewBag.Statuses = new[] { "Pending", "Paid", "Failed", "Refunded" };

        return View(payments);
    }

    public async Task<IActionResult> Settings()
    {
        var settings = await _context.PaymentSettings.ToListAsync();
        
        // Fetch Bank List from VietQR
        try 
        {
            using var client = new HttpClient();
            var response = await client.GetAsync("https://api.vietqr.io/v2/banks");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var bankData = JsonSerializer.Deserialize<BankResponse>(content);
                ViewBag.Banks = bankData?.data;
            }
        }
        catch { /* Fallback to empty if API down */ }

        // Seed default if empty
        if (!settings.Any())
        {
            _context.PaymentSettings.AddRange(new List<Models.PaymentSetting>
            {
                new Models.PaymentSetting { MethodName = "COD", Description = "Thanh toán bằng tiền mặt khi nhận hàng" },
                new Models.PaymentSetting { MethodName = "Chuyển khoản", Description = "Chuyển khoản ngân hàng qua mã QR" }
            });
            await _context.SaveChangesAsync();
            settings = await _context.PaymentSettings.ToListAsync();
        }
        
        return View(settings);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSetting(int id, bool isEnabled, string bankName, string accountNumber, string accountHolder, string? bankCode, IFormFile? qrCode)
    {
        var setting = await _context.PaymentSettings.FindAsync(id);
        if (setting == null) return NotFound();

        setting.IsEnabled = isEnabled;
        setting.BankName = bankName;
        setting.AccountNumber = accountNumber;
        setting.AccountHolder = accountHolder;
        setting.BankCode = bankCode;

        if (qrCode != null && qrCode.Length > 0)
        {
            var fileName = $"qr_{id}{Path.GetExtension(qrCode.FileName)}";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/payments", fileName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await qrCode.CopyToAsync(stream);
            }
            setting.QRCodeUrl = $"/img/payments/{fileName}";
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật cấu hình thành công!";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
            return NotFound();

        payment.PaymentStatus = status;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật trạng thái thanh toán thành công!";
        return RedirectToAction(nameof(Index));
    }
}
