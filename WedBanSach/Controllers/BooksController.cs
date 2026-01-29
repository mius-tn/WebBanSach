using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

public class BooksController : Controller
{
    private readonly BookStoreDbContext _context;

    public BooksController(BookStoreDbContext context)
    {
        _context = context;
    }

    [Route("chi-tiet-san-pham/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        if (id <= 0) return NotFound();

        var book = await _context.Books
            .Include(b => b.BookImages)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.Publisher)
            .Include(b => b.BookCategories).ThenInclude(bc => bc.Category)
            .Include(b => b.Reviews).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(b => b.BookID == id && b.Status == "Active");

        if (book == null)
        {
            return NotFound();
        }

        // Get related books (same category)
        var categoryIds = book.BookCategories.Select(bc => bc.CategoryID).ToList();
        var relatedBooks = await _context.Books
            .Include(b => b.BookImages)
            .Where(b => b.BookCategories.Any(bc => categoryIds.Contains(bc.CategoryID)) && b.BookID != id && b.Status == "Active")
            .OrderBy(r => Guid.NewGuid()) // Randomize
            .Take(10)
            .ToListAsync();

        // Calculate Average Rating and Count
        double averageRating = 0;
        int ratingCount = 0;
        if (book.Reviews != null && book.Reviews.Any())
        {
            averageRating = book.Reviews.Average(r => r.Rating);
            ratingCount = book.Reviews.Count;
        }

        // Calculate Sold Count from OrderDetails (Completed orders)
        // Note: Assuming 'Completed' is the status for sold items. Adjust if needed.
        var soldCount = await _context.OrderDetails
            .Where(od => od.BookID == id && od.Order.OrderStatus == "Completed")
            .SumAsync(od => (int?)od.Quantity) ?? 0;

        // Get Active Promotions (Mocking logic closest to reality: valid date range)
        var promotions = await _context.Promotions
            .Where(p => (p.StartDate == null || p.StartDate <= DateTime.Now) && 
                        (p.EndDate == null || p.EndDate >= DateTime.Now))
            .Take(3)
            .ToListAsync();

        ViewBag.RelatedBooks = relatedBooks;
        ViewBag.AverageRating = averageRating;
        ViewBag.RatingCount = ratingCount;
        ViewBag.SoldCount = soldCount;
        ViewBag.Promotions = promotions;

        // Fetch user default address
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (int.TryParse(userIdStr, out int userId))
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                ViewBag.UserProvinceCode = user.ProvinceCode;
                ViewBag.UserProvinceName = user.ProvinceName;
                ViewBag.UserDistrictCode = user.DistrictCode;
                ViewBag.UserDistrictName = user.DistrictName;
                ViewBag.UserWardCode = user.WardCode;
                ViewBag.UserWardCode = user.WardCode;
                ViewBag.UserWardName = user.WardName;
                ViewBag.UserHouseNumber = user.HouseNumber;

                // Check if user can review (Bought + Completed)
                // Optional: Check if already reviewed? User prompt didn't strictly say "hide if reviewed", just "show if bought".
                var hasPurchased = await _context.OrderDetails
                    .AnyAsync(od => od.BookID == id && od.Order.UserID == userId && 
                                   (od.Order.OrderStatus == "Completed" || od.Order.OrderStatus == "Hoàn tất"));
                ViewBag.CanReview = hasPurchased;
            }
            else
            {
                ViewBag.CanReview = false;
            }
        }

        return View(book);
    }

    [Route("danh-muc/{id?}")]
    public async Task<IActionResult> Category(string? id, string sortOrder, int page = 1, int pageSize = 12, string priceRange = "", string coverType = "", string searchTerm = "")
    {
        var booksQuery = _context.Books
            .Include(b => b.BookImages)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.Reviews)
            .Where(b => b.Status == "Active");

        // FILTER: Search Term
        if (!string.IsNullOrEmpty(searchTerm))
        {
            booksQuery = booksQuery.Where(b => b.Title.Contains(searchTerm) || 
                                               (b.ISBN != null && b.ISBN.Contains(searchTerm)) ||
                                               b.BookAuthors.Any(ba => ba.Author.AuthorName.Contains(searchTerm)));
        }

        int? categoryId = null;
        string? categoryName = null;

        if (!string.IsNullOrEmpty(id))
        {
            // Attempt to find by Slug
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == id);
            
            if (category == null)
            {
                // Fallback: Try parse as ID (legacy URL support)
                if (int.TryParse(id, out int parsedId))
                {
                    category = await _context.Categories.FindAsync(parsedId);
                }
            }

            if (category != null)
            {
                categoryId = category.CategoryID;
                categoryName = category.CategoryName;
                
                // Filter books by this Category
                booksQuery = booksQuery.Where(b => b.BookCategories.Any(bc => bc.CategoryID == categoryId));
            }
        }

        if (categoryId.HasValue)
        {
             ViewBag.CategoryName = categoryName;
             ViewBag.CategoryId = categoryId; // Keep ID for potential internal logic if needed
             ViewBag.CategorySlug = id; // Pass slug back for view generation
        }
        else
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                ViewBag.CategoryName = $"Kết quả tìm kiếm: {searchTerm}";
            }
            else
            {
                ViewBag.CategoryName = "Tất cả sản phẩm";
            }
        }

        // FILTER: Price Range (single selection)
        if (!string.IsNullOrEmpty(priceRange))
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(Book), "b");
            var discountPriceExp = System.Linq.Expressions.Expression.Property(parameter, "DiscountPrice");
            var priceExp = System.Linq.Expressions.Expression.Property(parameter, "Price");
            var effectivePriceExp = System.Linq.Expressions.Expression.Coalesce(discountPriceExp, priceExp);

            decimal min = 0; 
            decimal max = decimal.MaxValue;

            if (priceRange == "0-150000") { max = 150000; }
            else if (priceRange == "150000-300000") { min = 150000; max = 300000; }
            else if (priceRange == "300000-500000") { min = 300000; max = 500000; }
            else if (priceRange == "500000-") { min = 500000; }

            var minConst = System.Linq.Expressions.Expression.Constant(min, typeof(decimal));
            var maxConst = System.Linq.Expressions.Expression.Constant(max, typeof(decimal));

            var ge = System.Linq.Expressions.Expression.GreaterThanOrEqual(effectivePriceExp, minConst);
            var le = System.Linq.Expressions.Expression.LessThanOrEqual(effectivePriceExp, maxConst);
            var rangeCheck = System.Linq.Expressions.Expression.AndAlso(ge, le);

            var lambda = System.Linq.Expressions.Expression.Lambda<Func<Book, bool>>(rangeCheck, parameter);
            booksQuery = booksQuery.Where(lambda);
        }

        // FILTER: Cover Type (single selection)
        if (!string.IsNullOrEmpty(coverType))
        {
            booksQuery = booksQuery.Where(b => b.CoverType == coverType);
        }

        // Sorting
        switch (sortOrder)
        {
            case "price_desc":
                booksQuery = booksQuery.OrderByDescending(b => b.DiscountPrice ?? b.Price);
                break;
            case "price_asc":
                booksQuery = booksQuery.OrderBy(b => b.DiscountPrice ?? b.Price);
                break;
            default: // Newest
                booksQuery = booksQuery.OrderByDescending(b => b.CreatedAt);
                break;
        }

        // Pagination
        var totalRecords = await booksQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        
        // Validate page number
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;
        
        var books = await booksQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SortOrder = sortOrder;
        ViewBag.SelectedPriceRange = priceRange;
        ViewBag.SelectedCoverType = coverType;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        
        // Also need to get Sidebar Categories (tree)
        // For simplicity, re-fetching all categories or use ViewComponent in Layout
        // Let's pass root categories to view via ViewBag for Sidebar
        var categories = await _context.Categories.Where(c => c.ParentCategoryID == null).Include(c => c.SubCategories).ToListAsync();
        ViewBag.Categories = categories;

        return View(books);
    }

    [HttpGet("tim-kiem/goi-y")]
    public async Task<IActionResult> SearchSuggestions(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Json(new List<object>());

        term = term.ToLower();

        // Search Books
        var books = await _context.Books
            .Include(b => b.BookImages)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Where(b => b.Title.ToLower().Contains(term) && b.Status == "Active")
            .Take(5)
            .Select(b => new
            {
                id = b.BookID,
                title = b.Title,
                image = b.BookImages.FirstOrDefault(i => i.IsMain).ImageUrl ?? "/images/default-book.png",
                price = b.DiscountPrice ?? b.Price,
                author = b.BookAuthors.FirstOrDefault().Author.AuthorName ?? "",
                type = "book"
            })
            .ToListAsync();

        // Search Authors (and get one representative book or just link to search)
        // For simplicity, let's just find books BY that author
        var authorBooks = await _context.Books
            .Include(b => b.BookImages)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Where(b => b.BookAuthors.Any(ba => ba.Author.AuthorName.ToLower().Contains(term)) && b.Status == "Active")
            .Take(3)
            .Select(b => new
            {
                id = b.BookID,
                title = b.Title,
                image = b.BookImages.FirstOrDefault(i => i.IsMain).ImageUrl ?? "/images/default-book.png",
                price = b.DiscountPrice ?? b.Price,
                author = b.BookAuthors.FirstOrDefault().Author.AuthorName ?? "",
                type = "author_match" // To distinguish if needed, but for now treat as product link
            })
            .ToListAsync();
            
        // Merge and dedup (by ID)
        var results = books.Concat(authorBooks)
                           .GroupBy(x => x.id)
                           .Select(g => g.First())
                           .Take(8)
                           .ToList();

        return Json(results);
    }
}
