using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;
using WedBanSach.Constants;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminPromotionCampaignsController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminPromotionCampaignsController(BookStoreDbContext context)
    {
        _context = context;
    }

    [Permission(SystemPermissions.Module_Product, SystemPermissions.Action_View)]
    public async Task<IActionResult> Index()
    {
        var campaigns = await _context.PromotionCampaigns
            .Include(c => c.CampaignBooks)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
        return View(campaigns);
    }

    [Permission(SystemPermissions.Module_Product, SystemPermissions.Action_View)]
    public async Task<IActionResult> Details(int id)
    {
        var campaign = await _context.PromotionCampaigns
            .Include(c => c.CampaignBooks)
                .ThenInclude(cb => cb.Book)
                    .ThenInclude(b => b.BookImages)
            .FirstOrDefaultAsync(m => m.CampaignID == id);

        if (campaign == null) return NotFound();

        return View(campaign);
    }

    [HttpGet]
    [Permission(SystemPermissions.Module_Product, SystemPermissions.Action_Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Books = await _context.Books
            .Include(b => b.BookImages)
            .Where(b => b.Status == "Active")
            .OrderBy(b => b.Title)
            .ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(SystemPermissions.Module_Product, SystemPermissions.Action_Create)]
    public async Task<IActionResult> Create(PromotionCampaign campaign, List<int> bookIds)
    {
        if (ModelState.IsValid)
        {
            campaign.CreatedAt = DateTime.Now;
            campaign.CreatedBy = HttpContext.Session.GetString("FullName") ?? "Admin";
            _context.PromotionCampaigns.Add(campaign);
            await _context.SaveChangesAsync();

            // Add selected books
            if (bookIds != null && bookIds.Any())
            {
                foreach (var bookId in bookIds)
                {
                    _context.CampaignBooks.Add(new CampaignBook
                    {
                        CampaignID = campaign.CampaignID,
                        BookID = bookId
                    });
                }
                await _context.SaveChangesAsync();
            }

            // Auto-activate if campaign is active and within date range
            if (campaign.IsActive && campaign.StartDate <= DateTime.Now && campaign.EndDate > DateTime.Now && bookIds != null && bookIds.Any())
            {
                await ApplyCampaignToBooks(campaign, bookIds);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Tạo chiến dịch khuyến mãi thành công.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Books = await _context.Books
            .Include(b => b.BookImages)
            .Where(b => b.Status == "Active")
            .OrderBy(b => b.Title)
            .ToListAsync();
        return View(campaign);
    }

    [HttpGet]
    [Permission(SystemPermissions.Module_Product, SystemPermissions.Action_Update)]
    public async Task<IActionResult> Edit(int id)
    {
        var campaign = await _context.PromotionCampaigns
            .Include(c => c.CampaignBooks)
            .FirstOrDefaultAsync(c => c.CampaignID == id);

        if (campaign == null) return NotFound();

        ViewBag.Books = await _context.Books
            .Include(b => b.BookImages)
            .Where(b => b.Status == "Active")
            .OrderBy(b => b.Title)
            .ToListAsync();
        ViewBag.SelectedBookIds = campaign.CampaignBooks.Select(cb => cb.BookID).ToList();

        return View(campaign);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(SystemPermissions.Module_Product, SystemPermissions.Action_Update)]
    public async Task<IActionResult> Edit(int id, PromotionCampaign campaign, List<int> bookIds)
    {
        if (id != campaign.CampaignID)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                // Determine active status changes
                var originalCampaign = await _context.PromotionCampaigns.AsNoTracking().FirstOrDefaultAsync(c => c.CampaignID == id);
                var oldBookIds = await _context.CampaignBooks
                    .Where(cb => cb.CampaignID == id)
                    .Select(cb => cb.BookID)
                    .ToListAsync();

                bool wasActive = originalCampaign != null && originalCampaign.IsActive && originalCampaign.StartDate <= DateTime.Now && originalCampaign.EndDate > DateTime.Now;
                bool isActiveNow = campaign.IsActive && campaign.StartDate <= DateTime.Now && campaign.EndDate > DateTime.Now;

                _context.Update(campaign);

                // Update book associations
                var existingLinks = await _context.CampaignBooks
                    .Where(cb => cb.CampaignID == id)
                    .ToListAsync();
                _context.CampaignBooks.RemoveRange(existingLinks);

                if (bookIds != null && bookIds.Any())
                {
                    foreach (var bookId in bookIds)
                    {
                        _context.CampaignBooks.Add(new CampaignBook
                        {
                            CampaignID = id,
                            BookID = bookId
                        });
                    }
                }

                // Sync book prices based on active state changes
                if (originalCampaign != null)
                {
                    if (wasActive && isActiveNow)
                    {
                        // Remove discount from books that are no longer in the campaign
                        var removedBookIds = oldBookIds.Except(bookIds ?? new List<int>()).ToList();
                        if (removedBookIds.Any())
                        {
                            await RemoveCampaignFromBooks(originalCampaign, removedBookIds);
                        }

                        // Apply/Update discount on the current campaign books
                        if (bookIds != null && bookIds.Any())
                        {
                            await ApplyCampaignToBooks(campaign, bookIds);
                        }
                    }
                    else if (wasActive && !isActiveNow)
                    {
                        // Remove discount from all old books
                        if (oldBookIds.Any())
                        {
                            await RemoveCampaignFromBooks(originalCampaign, oldBookIds);
                        }
                    }
                    else if (!wasActive && isActiveNow)
                    {
                        // Apply discount to all current books
                        if (bookIds != null && bookIds.Any())
                        {
                            await ApplyCampaignToBooks(campaign, bookIds);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật chiến dịch thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.PromotionCampaigns.AnyAsync(c => c.CampaignID == id))
                    return NotFound();
                throw;
            }
        }

        ViewBag.Books = await _context.Books
            .Include(b => b.BookImages)
            .Where(b => b.Status == "Active")
            .OrderBy(b => b.Title)
            .ToListAsync();
        ViewBag.SelectedBookIds = bookIds ?? new List<int>();
        return View(campaign);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(SystemPermissions.Module_Product, SystemPermissions.Action_Update)]
    public async Task<IActionResult> Activate(int id)
    {
        var campaign = await _context.PromotionCampaigns
            .Include(c => c.CampaignBooks)
            .FirstOrDefaultAsync(c => c.CampaignID == id);

        if (campaign == null) return NotFound();

        campaign.IsActive = true;
        var bookIds = campaign.CampaignBooks.Select(cb => cb.BookID).ToList();

        await ApplyCampaignToBooks(campaign, bookIds);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã kích hoạt chiến dịch \"{campaign.Name}\" và áp dụng giảm giá cho {bookIds.Count} sách.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(SystemPermissions.Module_Product, SystemPermissions.Action_Update)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var campaign = await _context.PromotionCampaigns
            .Include(c => c.CampaignBooks)
            .FirstOrDefaultAsync(c => c.CampaignID == id);

        if (campaign == null) return NotFound();

        campaign.IsActive = false;
        var bookIds = campaign.CampaignBooks.Select(cb => cb.BookID).ToList();

        await RemoveCampaignFromBooks(campaign, bookIds);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã hủy kích hoạt chiến dịch \"{campaign.Name}\" và gỡ giảm giá cho {bookIds.Count} sách.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(SystemPermissions.Module_Product, SystemPermissions.Action_Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var campaign = await _context.PromotionCampaigns
            .Include(c => c.CampaignBooks)
            .FirstOrDefaultAsync(c => c.CampaignID == id);

        if (campaign == null) return NotFound();

        // Remove promotions from books if campaign was active
        if (campaign.IsActive)
        {
            var bookIds = campaign.CampaignBooks.Select(cb => cb.BookID).ToList();
            await RemoveCampaignFromBooks(campaign, bookIds);
        }

        _context.PromotionCampaigns.Remove(campaign);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa chiến dịch \"{campaign.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Apply campaign discount to all selected books
    /// </summary>
    private async Task ApplyCampaignToBooks(PromotionCampaign campaign, List<int> bookIds)
    {
        if (bookIds == null || !bookIds.Any()) return;

        var books = await _context.Books.Where(b => bookIds.Contains(b.BookID)).ToListAsync();
        var userName = HttpContext.Session.GetString("FullName") ?? "Admin";

        foreach (var book in books)
        {
            var oldPrice = book.CurrentPrice;

            // Calculate sale price
            decimal salePrice;
            int salePercent;
            if (campaign.DiscountType == "Percentage")
            {
                salePercent = (int)campaign.DiscountValue;
                salePrice = book.OriginalPrice * (1 - campaign.DiscountValue / 100m);
            }
            else // FixedAmount
            {
                salePrice = Math.Max(0, book.OriginalPrice - campaign.DiscountValue);
                salePercent = book.OriginalPrice > 0
                    ? (int)Math.Round((campaign.DiscountValue / book.OriginalPrice) * 100)
                    : 0;
            }

            book.SalePercent = salePercent;
            book.SalePrice = salePrice;
            book.SaleStartDate = campaign.StartDate;
            book.SaleEndDate = campaign.EndDate;
            book.IsPromotionActive = true;
            book.CurrentPrice = salePrice;

            // Record price history
            _context.PriceHistories.Add(new PriceHistory
            {
                BookID = book.BookID,
                OldPrice = oldPrice,
                NewPrice = salePrice,
                ChangeType = "PromotionStart",
                ChangedBy = userName,
                ChangedAt = DateTime.Now,
                Reason = $"Chiến dịch \"{campaign.Name}\" - Giảm {(campaign.DiscountType == "Percentage" ? $"{campaign.DiscountValue}%" : $"{campaign.DiscountValue:N0}đ")}"
            });
        }
    }

    /// <summary>
    /// Remove campaign discount from books and restore original price
    /// </summary>
    private async Task RemoveCampaignFromBooks(PromotionCampaign campaign, List<int> bookIds)
    {
        if (bookIds == null || !bookIds.Any()) return;

        var books = await _context.Books.Where(b => bookIds.Contains(b.BookID)).ToListAsync();
        var userName = HttpContext.Session.GetString("FullName") ?? "Admin";

        foreach (var book in books)
        {
            var oldPrice = book.CurrentPrice;

            book.SalePercent = null;
            book.SalePrice = null;
            book.SaleStartDate = null;
            book.SaleEndDate = null;
            book.IsPromotionActive = false;
            book.CurrentPrice = book.OriginalPrice;

            _context.PriceHistories.Add(new PriceHistory
            {
                BookID = book.BookID,
                OldPrice = oldPrice,
                NewPrice = book.OriginalPrice,
                ChangeType = "PromotionEnd",
                ChangedBy = userName,
                ChangedAt = DateTime.Now,
                Reason = $"Hủy chiến dịch \"{campaign.Name}\""
            });
        }
    }
}
