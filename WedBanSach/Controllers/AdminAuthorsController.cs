using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminAuthorsController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminAuthorsController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchTerm = "")
    {
        var query = _context.Authors.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(a => a.AuthorName.Contains(searchTerm));
        }

        var authors = await query.OrderBy(a => a.AuthorName).ToListAsync();
        ViewBag.SearchTerm = searchTerm;
        return View(authors);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Author author)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context.Add(author);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm tác giả thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi thêm tác giả: {ex.Message}";
                return View(author);
            }
        }
        return View(author);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var author = await _context.Authors.FindAsync(id);
        if (author == null)
            return NotFound();
        return View(author);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Author author)
    {
        if (id != author.AuthorID)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(author);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật tác giả thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(author.AuthorID))
                    return NotFound();
                TempData["Error"] = "Lỗi: Dữ liệu đã bị thay đổi bởi người khác. Vui lòng thử lại.";
                return View(author);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi cập nhật tác giả: {ex.Message}";
                return View(author);
            }
        }
        return View(author);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var author = await _context.Authors.FindAsync(id);
        if (author != null)
        {
            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa tác giả thành công!";
        }
        return RedirectToAction(nameof(Index));
    }

    private bool AuthorExists(int id)
    {
        return _context.Authors.Any(e => e.AuthorID == id);
    }
}
