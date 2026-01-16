using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminPromotionsController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminPromotionsController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var promotions = await _context.Promotions
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
        return View(promotions);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Promotion promotion)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context.Add(promotion);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi thêm khuyến mãi: {ex.Message}";
                return View(promotion);
            }
        }
        return View(promotion);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var promotion = await _context.Promotions.FindAsync(id);
        if (promotion == null)
            return NotFound();
        return View(promotion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Promotion promotion)
    {
        if (id != promotion.PromotionID)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(promotion);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PromotionExists(promotion.PromotionID))
                    return NotFound();
                TempData["Error"] = "Lỗi: Dữ liệu đã bị thay đổi bởi người khác. Vui lòng thử lại.";
                return View(promotion);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi cập nhật khuyến mãi: {ex.Message}";
                return View(promotion);
            }
        }
        return View(promotion);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var promotion = await _context.Promotions.FindAsync(id);
        if (promotion != null)
        {
            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa khuyến mãi thành công!";
        }
        return RedirectToAction(nameof(Index));
    }

    private bool PromotionExists(int id)
    {
        return _context.Promotions.Any(e => e.PromotionID == id);
    }
}
