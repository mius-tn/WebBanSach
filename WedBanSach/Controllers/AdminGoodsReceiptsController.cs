using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;
using WedBanSach.Constants;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminGoodsReceiptsController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminGoodsReceiptsController(BookStoreDbContext context)
    {
        _context = context;
    }

    [Permission(SystemPermissions.Module_Inventory, SystemPermissions.Action_View)]
    public async Task<IActionResult> Index()
    {
        var receipts = await _context.GoodsReceipts
            .OrderByDescending(g => g.EntryDate)
            .ToListAsync();
        return View(receipts);
    }

    [Permission(SystemPermissions.Module_Inventory, SystemPermissions.Action_View)]
    public async Task<IActionResult> Details(int id)
    {
        var receipt = await _context.GoodsReceipts
            .Include(g => g.GoodsReceiptDetails)
                .ThenInclude(d => d.Book)
            .FirstOrDefaultAsync(m => m.ReceiptID == id);

        if (receipt == null) return NotFound();

        return View(receipt);
    }

    [HttpGet]
    [Permission(SystemPermissions.Module_Inventory, SystemPermissions.Action_Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Books = await _context.Books.Where(b => b.Status == "Active").ToListAsync();
        return View();
    }

    [HttpPost]
    [Permission(SystemPermissions.Module_Inventory, SystemPermissions.Action_Create)]
    public async Task<IActionResult> Create(GoodsReceipt receipt, List<int> BookIDs, List<int> Quantities, List<decimal> UnitPrices)
    {
        if (BookIDs == null || BookIDs.Count == 0)
        {
            TempData["Error"] = "Vui lòng thêm ít nhất một sản phẩm.";
            ViewBag.Books = await _context.Books.Where(b => b.Status == "Active").ToListAsync();
            return View(receipt);
        }

        receipt.EntryDate = DateTime.Now;
        receipt.TotalAmount = 0;
        receipt.EnteredBy = HttpContext.Session.GetString("AdminUsername") ?? "System";

        _context.GoodsReceipts.Add(receipt);
        await _context.SaveChangesAsync(); // Save to get ReceiptID

        decimal totalAmount = 0;
        for (int i = 0; i < BookIDs.Count; i++)
        {
            if (Quantities[i] > 0)
            {
                var detail = new GoodsReceiptDetail
                {
                    ReceiptID = receipt.ReceiptID,
                    BookID = BookIDs[i],
                    Quantity = Quantities[i],
                    UnitPrice = UnitPrices[i]
                };
                totalAmount += (Quantities[i] * UnitPrices[i]);
                _context.GoodsReceiptDetails.Add(detail);

                // Update Book Stock
                var book = await _context.Books.FindAsync(BookIDs[i]);
                if (book != null)
                {
                    book.TotalStock += Quantities[i];
                    
                    // Add inventory log
                    var log = new InventoryLog
                    {
                        BookID = book.BookID,
                        ChangeQuantity = Quantities[i],
                        Reason = $"Nhập kho - Phiếu #{receipt.ReceiptID}",
                        CreatedAt = DateTime.Now
                    };
                    _context.InventoryLogs.Add(log);
                }
                
                // Note: Warehouse logic removed as GoodsReceipt doesn't link to Warehouse directly right now.
            }
        }

        receipt.TotalAmount = totalAmount;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Nhập kho thành công.";
        return RedirectToAction(nameof(Index));
    }
}
