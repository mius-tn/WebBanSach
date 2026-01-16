using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.ViewComponents;

public class CategoryMenuViewComponent : ViewComponent
{
    private readonly BookStoreDbContext _context;

    public CategoryMenuViewComponent(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Fetch categories that are top-level parents (ParentCategoryID == null) 
        // OR have non-null children. 
        // For simpler display, we'll just fetch ALL and group them in memory 
        // or let the view handle the hierarchy locally if the dataset is small.
        // But better to fetch hierarchy here.
        
        var categories = await _context.Categories
            .Include(c => c.SubCategories)
            .Where(c => c.ParentCategoryID == null) // Root categories
            .ToListAsync();

        return View(categories);
    }
}
