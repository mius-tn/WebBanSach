using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminShippingMethodsController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminShippingMethodsController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.ShippingMethods.ToListAsync());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Price,estimatedDays")] ShippingMethod shippingMethod)
    {
        if (ModelState.IsValid)
        {
            _context.Add(shippingMethod);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(shippingMethod);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var shippingMethod = await _context.ShippingMethods.FindAsync(id);
        if (shippingMethod == null) return NotFound();
        
        return View(shippingMethod);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ShippingMethodID,Name,Price,estimatedDays")] ShippingMethod shippingMethod)
    {
        if (id != shippingMethod.ShippingMethodID) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(shippingMethod);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShippingMethodExists(shippingMethod.ShippingMethodID)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(shippingMethod);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var shippingMethod = await _context.ShippingMethods.FindAsync(id);
        if (shippingMethod != null)
        {
            _context.ShippingMethods.Remove(shippingMethod);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool ShippingMethodExists(int id)
    {
        return _context.ShippingMethods.Any(e => e.ShippingMethodID == id);
    }
}
