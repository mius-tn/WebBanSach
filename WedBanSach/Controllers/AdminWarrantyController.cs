using Microsoft.AspNetCore.Mvc;
using WedBanSach.Attributes;
using WedBanSach.Services;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminWarrantyController : Controller
{
    private readonly IWarrantyRequestService _warrantyRequestService;

    public AdminWarrantyController(IWarrantyRequestService warrantyRequestService)
    {
        _warrantyRequestService = warrantyRequestService;
    }

    public async Task<IActionResult> Index()
    {
        var requests = await _warrantyRequestService.GetAllRequestsAsync();
        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return Json(new { success = false, message = "Trạng thái không hợp lệ." });
        }

        var result = await _warrantyRequestService.UpdateStatusAsync(id, status);
        if (result)
        {
            return Json(new { success = true, message = "Cập nhật trạng thái bảo hành thành công!" });
        }
        return Json(new { success = false, message = "Cập nhật trạng thái bảo hành thất bại." });
    }
}
