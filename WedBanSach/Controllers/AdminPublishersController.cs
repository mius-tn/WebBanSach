using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminPublishersController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminPublishersController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchTerm = "")
    {
        var query = _context.Publishers.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p => p.PublisherName.Contains(searchTerm) || 
                                   (p.Address != null && p.Address.Contains(searchTerm)));
        }

        var publishers = await query.OrderBy(p => p.PublisherName).ToListAsync();
        ViewBag.SearchTerm = searchTerm;
        return View(publishers);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Publisher publisher)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context.Add(publisher);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm nhà xuất bản thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi thêm nhà xuất bản: {ex.Message}";
                return View(publisher);
            }
        }
        return View(publisher);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var publisher = await _context.Publishers.FindAsync(id);
        if (publisher == null)
            return NotFound();
        return View(publisher);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Publisher publisher)
    {
        if (id != publisher.PublisherID)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(publisher);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật nhà xuất bản thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PublisherExists(publisher.PublisherID))
                    return NotFound();
                TempData["Error"] = "Lỗi: Dữ liệu đã bị thay đổi bởi người khác. Vui lòng thử lại.";
                return View(publisher);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi cập nhật nhà xuất bản: {ex.Message}";
                return View(publisher);
            }
        }
        return View(publisher);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var publisher = await _context.Publishers.FindAsync(id);
        if (publisher != null)
        {
            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa nhà xuất bản thành công!";
        }
        return RedirectToAction(nameof(Index));
    }

    private bool PublisherExists(int id)
    {
        return _context.Publishers.Any(e => e.PublisherID == id);
    }
}
