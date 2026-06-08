using Microsoft.AspNetCore.Mvc;
using WedBanSach.Services;

namespace WedBanSach.Controllers;

public class PolicyController : Controller
{
    private readonly IPolicyService _policyService;

    public PolicyController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _policyService.GetAllCategoriesAsync();
        return View(categories);
    }

    public async Task<IActionResult> Exchange()
    {
        var category = await _policyService.GetCategoryBySlugAsync("doi-hang");
        ViewData["Title"] = "Chính sách Đổi hàng";
        return View("Detail", category);
    }

    public async Task<IActionResult> Return()
    {
        var category = await _policyService.GetCategoryBySlugAsync("tra-hang");
        ViewData["Title"] = "Chính sách Trả hàng";
        return View("Detail", category);
    }

    public async Task<IActionResult> Refund()
    {
        var category = await _policyService.GetCategoryBySlugAsync("hoan-tien");
        ViewData["Title"] = "Chính sách Hoàn tiền";
        return View("Detail", category);
    }

    public async Task<IActionResult> Warranty()
    {
        var category = await _policyService.GetCategoryBySlugAsync("bao-hanh");
        ViewData["Title"] = "Chính sách Bảo hành";
        return View("Detail", category);
    }
}
