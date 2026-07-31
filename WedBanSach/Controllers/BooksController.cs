using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;
using WedBanSach.ViewModels;

namespace WedBanSach.Controllers;

public class BooksController : Controller
{
    private readonly BookStoreDbContext _context;
    private readonly WedBanSach.Services.Apriori.IAprioriService _aprioriService;

    public BooksController(BookStoreDbContext context, WedBanSach.Services.Apriori.IAprioriService aprioriService)
    {
        _context = context;
        _aprioriService = aprioriService;
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
            .Include(b => b.Reviews)
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

        // Get Price History
        var priceHistory = await _context.PriceHistories
            .Where(ph => ph.BookID == id)
            .OrderByDescending(ph => ph.ChangedAt)
            .ToListAsync();

        // Lowest Price in 30 days
        var thirtyDaysAgo = DateTime.Now.AddDays(-30);
        
        // The lowest price could be the current price, or an old price within the last 30 days.
        // We look at all price history records in the last 30 days + current price
        var pricesIn30Days = priceHistory
            .Where(ph => ph.ChangedAt >= thirtyDaysAgo)
            .Select(ph => ph.NewPrice)
            .ToList();
            
        pricesIn30Days.Add(book.CurrentPrice);
        
        // Also consider the price that was effective 30 days ago (the latest record before thirtyDaysAgo)
        var price30DaysAgo = priceHistory.FirstOrDefault(ph => ph.ChangedAt < thirtyDaysAgo)?.NewPrice;
        if (price30DaysAgo.HasValue)
        {
            pricesIn30Days.Add(price30DaysAgo.Value);
        }

        var lowestPrice30Days = pricesIn30Days.Min();

        ViewBag.RelatedBooks = relatedBooks;
        ViewBag.AverageRating = averageRating;
        ViewBag.RatingCount = ratingCount;
        ViewBag.SoldCount = soldCount;

        // Apriori Recommendations
        var frequentlyBoughtTogether = await _aprioriService.GetRecommendationsForBookAsync(id, 5);
        ViewBag.AprioriRecommendations = frequentlyBoughtTogether;

        ViewBag.Promotions = promotions;
        ViewBag.PriceHistory = priceHistory;
        ViewBag.LowestPrice30Days = lowestPrice30Days;

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
            // 1. Accent-insensitive and case-insensitive search using SQL_Latin1_General_CP1_CI_AI
            var accentInsensitiveQuery = booksQuery.Where(b => 
                EF.Functions.Collate(b.Title, "SQL_Latin1_General_CP1_CI_AI").Contains(searchTerm) || 
                (b.ISBN != null && EF.Functions.Collate(b.ISBN, "SQL_Latin1_General_CP1_CI_AI").Contains(searchTerm)) ||
                b.BookAuthors.Any(ba => EF.Functions.Collate(ba.Author.AuthorName, "SQL_Latin1_General_CP1_CI_AI").Contains(searchTerm))
            );

            var count = await accentInsensitiveQuery.CountAsync();
            if (count > 0)
            {
                booksQuery = accentInsensitiveQuery;
            }
            else
            {
                // 2. Similar search fallback (split search terms into individual words)
                var words = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Where(w => w.Length >= 2)
                                      .ToList();

                if (words.Any())
                {
                    // Build dynamic OR query using Expression trees
                    var parameter = System.Linq.Expressions.Expression.Parameter(typeof(Book), "b");
                    System.Linq.Expressions.Expression? body = null;

                    var functionsProp = typeof(EF).GetProperty("Functions", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
                    var functionsExpr = System.Linq.Expressions.Expression.Property(null, functionsProp);
                    var collateMethod = typeof(RelationalDbFunctionsExtensions).GetMethods().First(m => m.Name == "Collate" && m.IsGenericMethod).MakeGenericMethod(typeof(string));
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;

                    foreach (var word in words)
                    {
                        var titleProp = System.Linq.Expressions.Expression.Property(parameter, "Title");
                        var collateCall = System.Linq.Expressions.Expression.Call(null, collateMethod, functionsExpr, titleProp, System.Linq.Expressions.Expression.Constant("SQL_Latin1_General_CP1_CI_AI"));
                        var containsCall = System.Linq.Expressions.Expression.Call(collateCall, containsMethod, System.Linq.Expressions.Expression.Constant(word));

                        if (body == null)
                        {
                            body = containsCall;
                        }
                        else
                        {
                            body = System.Linq.Expressions.Expression.OrElse(body, containsCall);
                        }
                    }

                    if (body != null)
                    {
                        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Book, bool>>(body, parameter);
                        var similarQuery = booksQuery.Where(lambda);
                        count = await similarQuery.CountAsync();
                        if (count > 0)
                        {
                            booksQuery = similarQuery;
                        }
                        else
                        {
                            // 3. Recommended/Suggested books fallback
                            ViewBag.IsSuggested = true;
                            booksQuery = _context.Books
                                .Include(b => b.BookImages)
                                .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                                .Include(b => b.Reviews)
                                .Where(b => b.Status == "Active")
                                .OrderByDescending(b => b.CurrentPrice < b.OriginalPrice ? (b.OriginalPrice - b.CurrentPrice) : 0);
                        }
                    }
                    else
                    {
                        ViewBag.IsSuggested = true;
                        booksQuery = _context.Books
                            .Include(b => b.BookImages)
                            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                            .Include(b => b.Reviews)
                            .Where(b => b.Status == "Active")
                            .OrderByDescending(b => b.CurrentPrice < b.OriginalPrice ? (b.OriginalPrice - b.CurrentPrice) : 0);
                    }
                }
                else
                {
                    ViewBag.IsSuggested = true;
                    booksQuery = _context.Books
                        .Include(b => b.BookImages)
                        .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                        .Include(b => b.Reviews)
                        .Where(b => b.Status == "Active")
                        .OrderByDescending(b => b.CurrentPrice < b.OriginalPrice ? (b.OriginalPrice - b.CurrentPrice) : 0);
                }
            }
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
        if (ViewBag.IsSuggested != true && !string.IsNullOrEmpty(priceRange))
        {
            decimal min = 0; 
            decimal? max = null;

            if (priceRange == "0-150000") { max = 150000; }
            else if (priceRange == "150000-300000") { min = 150000; max = 300000; }
            else if (priceRange == "300000-500000") { min = 300000; max = 500000; }
            else if (priceRange == "500000-") { min = 500000; }

            if (max.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.CurrentPrice >= min && b.CurrentPrice <= max.Value);
            }
            else
            {
                booksQuery = booksQuery.Where(b => b.CurrentPrice >= min);
            }
        }

        // FILTER: Cover Type (single selection)
        if (ViewBag.IsSuggested != true && !string.IsNullOrEmpty(coverType))
        {
            booksQuery = booksQuery.Where(b => b.CoverType == coverType);
        }

        // Sorting
        switch (sortOrder)
        {
            case "price_desc":
                booksQuery = booksQuery.OrderByDescending(b => b.CurrentPrice);
                break;
            case "price_asc":
                booksQuery = booksQuery.OrderBy(b => b.CurrentPrice);
                break;
            case "newest":
                booksQuery = booksQuery.OrderByDescending(b => b.CreatedAt);
                break;
            default: // Best Selling Week (Default)
                var sevenDaysAgo = DateTime.Today.AddDays(-7);
                booksQuery = booksQuery.OrderByDescending(b => b.OrderDetails
                    .Where(od => od.Order.OrderDate >= sevenDaysAgo && 
                            (od.Order.OrderStatus == "Completed" || od.Order.OrderStatus == "Hoàn tất"))
                    .Select(od => od.Quantity)
                    .Sum());
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

    [Route("flash-sale")]
    public async Task<IActionResult> FlashSale(int? campaignId, string sortOrder = "", string priceRange = "", int page = 1, int pageSize = 20)
    {
        var now = DateTime.Now;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);

        // Load all campaigns that overlap with today (active or scheduled)
        var allCampaigns = await _context.PromotionCampaigns
            .Include(c => c.CampaignBooks)
            .Where(c => c.EndDate >= todayStart && c.StartDate <= todayEnd.AddDays(7))
            .OrderBy(c => c.StartDate)
            .ToListAsync();

        // Determine which campaign to display
        PromotionCampaign? selectedCampaign = null;

        if (campaignId.HasValue)
        {
            selectedCampaign = allCampaigns.FirstOrDefault(c => c.CampaignID == campaignId.Value);
        }

        // Auto-select: prefer currently active campaign
        if (selectedCampaign == null)
        {
            selectedCampaign = allCampaigns.FirstOrDefault(c => c.IsActive && c.StartDate <= now && c.EndDate > now);
        }

        // Fallback: first upcoming campaign
        if (selectedCampaign == null)
        {
            selectedCampaign = allCampaigns.FirstOrDefault(c => c.StartDate > now);
        }

        // Fallback: most recent ended campaign
        if (selectedCampaign == null)
        {
            selectedCampaign = allCampaigns.LastOrDefault();
        }

        // Build books query
        IQueryable<Book> booksQuery;

        if (selectedCampaign != null)
        {
            var bookIds = selectedCampaign.CampaignBooks.Select(cb => cb.BookID).ToList();
            booksQuery = _context.Books
                .Include(b => b.BookImages)
                .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                .Include(b => b.Reviews)
                .Where(b => bookIds.Contains(b.BookID) && b.Status == "Active");
        }
        else
        {
            // No campaigns at all — show all discounted books
            booksQuery = _context.Books
                .Include(b => b.BookImages)
                .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                .Include(b => b.Reviews)
                .Where(b => b.Status == "Active" && (b.IsPromotionActive || b.CurrentPrice < b.OriginalPrice));
        }

        // Price range filter
        if (!string.IsNullOrEmpty(priceRange))
        {
            if (priceRange == "0-150000")
            {
                booksQuery = booksQuery.Where(b => b.CurrentPrice <= 150000);
            }
            else if (priceRange == "150000-300000")
            {
                booksQuery = booksQuery.Where(b => b.CurrentPrice >= 150000 && b.CurrentPrice <= 300000);
            }
            else if (priceRange == "300000-500000")
            {
                booksQuery = booksQuery.Where(b => b.CurrentPrice >= 300000 && b.CurrentPrice <= 500000);
            }
            else if (priceRange == "500000-")
            {
                booksQuery = booksQuery.Where(b => b.CurrentPrice >= 500000);
            }
        }

        // Sorting
        switch (sortOrder)
        {
            case "discount_desc":
                booksQuery = booksQuery.OrderByDescending(b => b.OriginalPrice > 0 ? (b.OriginalPrice - b.CurrentPrice) / b.OriginalPrice : 0);
                break;
            case "price_asc":
                booksQuery = booksQuery.OrderBy(b => b.CurrentPrice);
                break;
            case "price_desc":
                booksQuery = booksQuery.OrderByDescending(b => b.CurrentPrice);
                break;
            default: // best_selling
                booksQuery = booksQuery.OrderByDescending(b => b.SoldStock);
                break;
        }

        // Pagination
        var totalRecords = await booksQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var books = await booksQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new FlashSaleViewModel
        {
            SelectedCampaign = selectedCampaign,
            AllCampaigns = allCampaigns,
            Books = books,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            SelectedCampaignId = selectedCampaign?.CampaignID,
            SortOrder = sortOrder,
            PriceRange = priceRange
        };

        return View(viewModel);
    }

    [HttpGet("tim-kiem/goi-y")]
    public async Task<IActionResult> 
        SearchSuggestions(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Json(new List<object>());

        term = term.ToLower();

        // Search Books
        var books = await _context.Books
            .Include(b => b.BookImages)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Where(b => EF.Functions.Collate(b.Title, "SQL_Latin1_General_CP1_CI_AI").Contains(term) && b.Status == "Active")
            .Take(5)
            .Select(b => new
            {
                id = b.BookID,
                title = b.Title,
                image = b.BookImages.FirstOrDefault(i => i.IsMain).ImageUrl ?? "/images/default-book.png",
                price = b.CurrentPrice,
                author = b.BookAuthors.FirstOrDefault().Author.AuthorName ?? "",
                type = "book"
            })
            .ToListAsync();

        // Search Authors (and get one representative book or just link to search)
        // For simplicity, let's just find books BY that author
        var authorBooks = await _context.Books
            .Include(b => b.BookImages)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Where(b => b.BookAuthors.Any(ba => EF.Functions.Collate(ba.Author.AuthorName, "SQL_Latin1_General_CP1_CI_AI").Contains(term)) && b.Status == "Active")
            .Take(3)
            .Select(b => new
            {
                id = b.BookID,
                title = b.Title,
                image = b.BookImages.FirstOrDefault(i => i.IsMain).ImageUrl ?? "/images/default-book.png",
                price = b.CurrentPrice,
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

    [HttpGet("gioi-thieu/hoi-chung-tuoi-thanh-xuan")]
    public async Task<IActionResult> RascalIntro()
    {
        var book = await _context.Books
            .Include(b => b.BookImages)
            .Include(b => b.Reviews)
            .FirstOrDefaultAsync(b => b.BookID == 13);

        if (book == null)
        {
            book = await _context.Books
                .Include(b => b.BookImages)
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync(b => b.Title.Contains("Hội Chứng Tuổi Thanh Xuân"));
        }

        if (book == null)
        {
            book = await _context.Books
                .Include(b => b.BookImages)
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync();
        }

        var allRascalBooks = await _context.Books
            .Include(b => b.BookImages)
            .Where(b => b.Title.Contains("Hội Chứng Tuổi Thanh Xuân"))
            .ToListAsync();

        ViewBag.AllRascalBooks = allRascalBooks;

        return View(book);
    }
}
