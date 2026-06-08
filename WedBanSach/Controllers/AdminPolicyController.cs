using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;
using WedBanSach.Services;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminPolicyController : Controller
{
    private readonly IPolicyService _policyService;
    private readonly BookStoreDbContext _context;

    public AdminPolicyController(IPolicyService policyService, BookStoreDbContext context)
    {
        _policyService = policyService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var policies = await _policyService.GetAllPoliciesAsync();
        var categories = await _policyService.GetAllCategoriesAsync();
        ViewBag.Categories = categories;
        return View(policies);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var categories = await _policyService.GetAllCategoriesAsync();
        ViewBag.Categories = categories;
        return View(new Policy());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Policy policy)
    {
        if (ModelState.IsValid)
        {
            var result = await _policyService.CreatePolicyAsync(policy);
            if (result)
            {
                TempData["Success"] = "Tạo chính sách mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo chính sách.");
        }

        var categories = await _policyService.GetAllCategoriesAsync();
        ViewBag.Categories = categories;
        return View(policy);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var policy = await _policyService.GetPolicyByIdAsync(id);
        if (policy == null) return NotFound();

        var categories = await _policyService.GetAllCategoriesAsync();
        ViewBag.Categories = categories;
        return View(policy);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Policy policy)
    {
        if (id != policy.Id) return BadRequest();

        if (ModelState.IsValid)
        {
            var result = await _policyService.UpdatePolicyAsync(policy);
            if (result)
            {
                TempData["Success"] = "Cập nhật chính sách thành công!";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật.");
        }

        var categories = await _policyService.GetAllCategoriesAsync();
        ViewBag.Categories = categories;
        return View(policy);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _policyService.DeletePolicyAsync(id);
        if (result)
        {
            return Json(new { success = true, message = "Xóa chính sách thành công!" });
        }
        return Json(new { success = false, message = "Không thể xóa chính sách." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var policy = await _policyService.GetPolicyByIdAsync(id);
        if (policy == null) return NotFound();

        policy.IsPublished = !policy.IsPublished;
        var result = await _policyService.UpdatePolicyAsync(policy);

        return Json(new { success = true, isPublished = policy.IsPublished });
    }

    // Category Quick CRUD for Ajax
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string slug, string? description)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(slug))
        {
            return Json(new { success = false, message = "Vui lòng nhập tên và đường dẫn Slug." });
        }

        var exists = await _context.PolicyCategories.AnyAsync(c => c.Slug == slug);
        if (exists)
        {
            return Json(new { success = false, message = "Đường dẫn Slug đã tồn tại." });
        }

        var cat = new PolicyCategory
        {
            Name = name,
            Slug = slug,
            Description = description,
            IsActive = true
        };

        var result = await _policyService.CreateCategoryAsync(cat);
        if (result)
        {
            return Json(new { success = true, message = "Tạo danh mục chính sách thành công!", id = cat.Id, name = cat.Name });
        }
        return Json(new { success = false, message = "Không thể lưu danh mục." });
    }
}
