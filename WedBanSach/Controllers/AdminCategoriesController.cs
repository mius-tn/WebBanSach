using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminCategoriesController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminCategoriesController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadParentCategories();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi thêm danh mục: {ex.Message}";
                await LoadParentCategories();
                return View(category);
            }
        }
        await LoadParentCategories();
        return View(category);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound();

        await LoadParentCategories(category.CategoryID);
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category category)
    {
        if (id != category.CategoryID)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.CategoryID))
                    return NotFound();
                TempData["Error"] = "Lỗi: Dữ liệu đã bị thay đổi bởi người khác. Vui lòng thử lại.";
                await LoadParentCategories(category.CategoryID);
                return View(category);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi cập nhật danh mục: {ex.Message}";
                await LoadParentCategories(category.CategoryID);
                return View(category);
            }
        }
        await LoadParentCategories(category.CategoryID);
        return View(category);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category == null)
            return NotFound();

        if (category.SubCategories.Any())
        {
            TempData["Error"] = "Không thể xóa danh mục có danh mục con!";
            return RedirectToAction(nameof(Index));
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Xóa danh mục thành công!";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadParentCategories(int? excludeId = null)
    {
        var query = _context.Categories.AsQueryable();
        if (excludeId.HasValue)
            query = query.Where(c => c.CategoryID != excludeId.Value);

        ViewBag.ParentCategories = new SelectList(await query.OrderBy(c => c.CategoryName).ToListAsync(),
            "CategoryID", "CategoryName");
    }

    private bool CategoryExists(int id)
    {
        return _context.Categories.Any(e => e.CategoryID == id);
    }
}
