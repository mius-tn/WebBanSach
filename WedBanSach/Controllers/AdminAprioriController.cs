using Microsoft.AspNetCore.Mvc;
using WedBanSach.Attributes;
using WedBanSach.Models.Apriori;
using WedBanSach.Services.Apriori;

namespace WedBanSach.Controllers;

[AuthorizeAdmin] // Ensure only staff/admin can access
[Route("Admin/Apriori")]
public class AdminAprioriController : Controller
{
    private readonly IAprioriService _aprioriService;

    public AdminAprioriController(IAprioriService aprioriService)
    {
        _aprioriService = aprioriService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var model = await _aprioriService.GetDashboardDataAsync();
        return View(model);
    }

    [HttpGet("Rules")]
    public async Task<IActionResult> Rules(int page = 1)
    {
        int pageSize = 20;
        var result = await _aprioriService.GetRulesAsync(page, pageSize);
        return View(result);
    }

    [HttpGet("Config")]
    public async Task<IActionResult> Config()
    {
        var config = await _aprioriService.GetConfigAsync();
        return View(config);
    }

    [HttpPost("Config")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Config(AprioriConfig model)
    {
        if (ModelState.IsValid)
        {
            await _aprioriService.UpdateConfigAsync(model);
            TempData["Success"] = "Cập nhật cấu hình thành công.";
            return RedirectToAction(nameof(Config));
        }
        return View(model);
    }

    [HttpPost("Train")]
    public async Task<IActionResult> Train()
    {
        // This will block the request, ideally should run as background task and return accepted.
        // For demonstration, we await it. In production, wrap in Hangfire FireAndForget.
        try
        {
            await _aprioriService.TrainModelAsync();
            TempData["Success"] = "Đã huấn luyện xong mô hình Apriori.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
        }
        
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("ExportCsv")]
    public async Task<IActionResult> ExportCsv()
    {
        // Get all rules (assuming page 1 with huge page size or we fetch all)
        var result = await _aprioriService.GetRulesAsync(1, 10000);
        
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("RuleId,Antecedent,Consequent,Support,Confidence,Lift,Score");
        
        foreach (var rule in result.Items)
        {
            builder.AppendLine($"{rule.Id},\"{rule.AntecedentNames}\",\"{rule.ConsequentNames}\",{rule.Support},{rule.Confidence},{rule.Lift},{rule.RecommendationScore}");
        }
        
        return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"AprioriRules_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}
