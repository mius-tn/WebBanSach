using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Services;

/// <summary>
/// Thuật toán: Content-Based Filtering với Weighted Feature Score
///
/// Score = Σ (tín hiệu × trọng số), chuẩn hóa về [0.0 → 1.0]
///
/// 5 tín hiệu:
///   [35%] Thể loại yêu thích  (FavoriteGenres)
///   [30%] Tác giả yêu thích   (FavoriteAuthors)
///   [20%] Lịch sử mua hàng    (OrderDetails → cùng thể loại)
///   [10%] Độ phổ biến          (SoldQuantity chuẩn hóa log)
///   [5%]  Phù hợp khoảng giá  (PreferredPriceRange)
/// </summary>
public class RecommendationService
{
    private readonly BookStoreDbContext _context;

    public RecommendationService(BookStoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Searches for books based on a textual query to be injected into the LLM context.
    /// </summary>
    public async Task<List<Book>> GetMatchingBooksAsync(string query, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await _context.Books
                .Include(b => b.BookImages)
                .Where(b => b.Status == "Active")
                .OrderByDescending(b => b.SoldQuantity)
                .Take(maxResults)
                .ToListAsync();
        }

        var normalizedQuery = query.ToLower().Trim();

        // Clean common conversational stop-words/fluff
        var fluffWords = new[] { "quyển", "cuốn", "sách", "tìm", "muốn", "mua", "giúp", "cho", "tớ", "bạn", "ad", "admin", "hay", "không", "có", "tư", "vấn", "được", "nhỉ", "nhé", "nha", "với", "xem", "tác", "phẩm", "bộ", "đọc", "bán", "chạy" };
        var cleanedQuery = normalizedQuery;
        foreach (var word in fluffWords)
        {
            cleanedQuery = System.Text.RegularExpressions.Regex.Replace(cleanedQuery, $@"\b{word}\b", "").Trim();
        }

        if (string.IsNullOrWhiteSpace(cleanedQuery))
            cleanedQuery = normalizedQuery;

        var books = await _context.Books
            .Include(b => b.BookImages)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookCategories).ThenInclude(bc => bc.Category)
            .Where(b => b.Status == "Active")
            .ToListAsync();

        return books
            .Where(b =>
                b.Title.ToLower().Contains(cleanedQuery) ||
                cleanedQuery.Contains(b.Title.ToLower()) ||
                (b.Description != null && b.Description.ToLower().Contains(cleanedQuery)) ||
                b.BookAuthors.Any(ba => ba.Author.AuthorName.ToLower().Contains(cleanedQuery)) ||
                b.BookCategories.Any(bc => bc.Category.CategoryName.ToLower().Contains(cleanedQuery))
            )
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Generates recommendations tailored to a user's preferences.
    /// Thuật toán: Content-Based Filtering với 5 tín hiệu có trọng số.
    /// </summary>
    public async Task<List<Book>> GetAIRecommendationsAsync(int? userId, int maxResults = 6)
    {
        // ── Load tất cả sách Active ──────────────────────────────────────
        var allBooks = await _context.Books
            .Include(b => b.BookImages)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookCategories).ThenInclude(bc => bc.Category)
            .Where(b => b.Status == "Active")
            .ToListAsync();

        if (!allBooks.Any())
            return new List<Book>();

        // ── Chuẩn hóa SoldQuantity (log scale để tránh outlier) ──────────
        int maxSold = allBooks.Max(b => b.SoldQuantity);
        if (maxSold == 0) maxSold = 1;

        // ── Load thông tin người dùng ─────────────────────────────────────
        List<string> favGenres  = new();
        List<string> favAuthors = new();
        (decimal min, decimal max) priceRange = (0, decimal.MaxValue);
        HashSet<int> purchasedCategoryIds = new();

        if (userId.HasValue)
        {
            // Tín hiệu 1+2+5: Sở thích đã lưu
            var preference = await _context.CustomerPreferences
                .FirstOrDefaultAsync(p => p.CustomerId == userId.Value);

            if (preference != null)
            {
                favGenres = preference.FavoriteGenres?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim().ToLower()).ToList() ?? new();

                favAuthors = preference.FavoriteAuthors?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Trim().ToLower()).ToList() ?? new();

                priceRange = ParsePriceRange(preference.PreferredPriceRange);
            }

            // Tín hiệu 3: Lịch sử mua hàng → lấy CategoryId đã mua
            var purchasedBookIds = await _context.OrderDetails
                .Where(od => od.Order.UserID == userId.Value)
                .Select(od => od.BookID)
                .Distinct()
                .ToListAsync();

            if (purchasedBookIds.Any())
            {
                purchasedCategoryIds = (await _context.Books
                    .Include(b => b.BookCategories)
                    .Where(b => purchasedBookIds.Contains(b.BookID))
                    .ToListAsync())
                    .SelectMany(b => b.BookCategories.Select(bc => bc.CategoryID))
                    .ToHashSet();
            }
        }

        // ── Tính Content-Based Score cho từng sách ────────────────────────
        //
        //   FinalScore = (s_genre  × 0.35)   ← thể loại yêu thích
        //              + (s_author × 0.30)   ← tác giả yêu thích
        //              + (s_history× 0.20)   ← lịch sử mua cùng thể loại
        //              + (s_popular× 0.10)   ← độ phổ biến (log scale)
        //              + (s_price  × 0.05)   ← phù hợp khoảng giá
        //
        var scored = allBooks.Select(b =>
        {
            // [35%] Tín hiệu 1: Thể loại yêu thích
            double s_genre = 0;
            if (favGenres.Any())
            {
                var bookGenres = b.BookCategories
                    .Select(bc => bc.Category.CategoryName.ToLower()).ToList();
                int matched = favGenres.Count(g => bookGenres.Any(bg => bg.Contains(g)));
                s_genre = (double)matched / favGenres.Count; // tỉ lệ khớp 0→1
            }

            // [30%] Tín hiệu 2: Tác giả yêu thích
            double s_author = 0;
            if (favAuthors.Any())
            {
                var bookAuthors = b.BookAuthors
                    .Select(ba => ba.Author.AuthorName.ToLower()).ToList();
                int matched = favAuthors.Count(a => bookAuthors.Any(ba => ba.Contains(a)));
                s_author = (double)matched / favAuthors.Count;
            }

            // [20%] Tín hiệu 3: Lịch sử mua hàng (cùng thể loại đã từng mua)
            double s_history = 0;
            if (purchasedCategoryIds.Any())
            {
                bool overlap = b.BookCategories.Any(bc => purchasedCategoryIds.Contains(bc.CategoryID));
                s_history = overlap ? 1.0 : 0.0;
            }

            // [10%] Tín hiệu 4: Độ phổ biến — log(1+sold) / log(1+max)
            // Dùng log để tránh sách bestseller "nuốt" hết điểm
            double s_popular = Math.Log10(1 + b.SoldQuantity) / Math.Log10(1 + maxSold);

            // [5%] Tín hiệu 5: Phù hợp khoảng giá mong muốn
            decimal effectivePrice = b.DiscountPrice ?? b.Price;
            double s_price = (effectivePrice >= priceRange.min && effectivePrice <= priceRange.max)
                ? 1.0 : 0.0;

            // ── Weighted sum → FinalScore ∈ [0, 1] ───────────────────────
            double finalScore = (s_genre   * 0.35)
                              + (s_author  * 0.30)
                              + (s_history * 0.20)
                              + (s_popular * 0.10)
                              + (s_price   * 0.05);

            return new { Book = b, Score = finalScore };
        })
        .OrderByDescending(x => x.Score)
        .ToList();

        // Với khách đăng nhập: lọc score > 0 trước
        var filtered = userId.HasValue
            ? scored.Where(x => x.Score > 0).ToList()
            : scored;

        var recommendedBooks = filtered.Select(x => x.Book).Take(maxResults).ToList();
        var scoreMap = scored.ToDictionary(x => x.Book.BookID, x => x.Score);

        // ── Fallback: bổ sung bestsellers nếu chưa đủ ────────────────────
        if (recommendedBooks.Count < maxResults)
        {
            var needed = maxResults - recommendedBooks.Count;
            var existingIds = recommendedBooks.Select(b => b.BookID).ToHashSet();
            var bestsellers = allBooks
                .Where(b => !existingIds.Contains(b.BookID))
                .OrderByDescending(b => b.SoldQuantity)
                .Take(needed)
                .ToList();
            recommendedBooks.AddRange(bestsellers);
        }

        // ── Lưu AIRecommendation với score thực vào DB ────────────────────
        foreach (var b in recommendedBooks)
        {
            double rawScore = scoreMap.GetValueOrDefault(b.BookID, 0.0);
            decimal dbScore = (decimal)Math.Round(Math.Clamp(rawScore, 0.0, 1.0), 4);

            _context.AIRecommendations.Add(new AIRecommendation
            {
                CustomerId          = userId,
                ProductId           = b.BookID,
                RecommendationScore = dbScore, // ← điểm thực [0.0000–1.0000]
                CreatedAt           = DateTime.Now
            });
        }
        await _context.SaveChangesAsync();

        return recommendedBooks;
    }

    /// <summary>
    /// Save customer preferences (favorite genres, authors, price ranges) based on browsing/chatting.
    /// </summary>
    public async Task SavePreferencesAsync(int userId, string genres, string authors, string priceRange)
    {
        var pref = await _context.CustomerPreferences.FirstOrDefaultAsync(p => p.CustomerId == userId);
        if (pref == null)
        {
            pref = new CustomerPreference { CustomerId = userId };
            _context.CustomerPreferences.Add(pref);
        }

        if (!string.IsNullOrEmpty(genres))     pref.FavoriteGenres = genres;
        if (!string.IsNullOrEmpty(authors))    pref.FavoriteAuthors = authors;
        if (!string.IsNullOrEmpty(priceRange)) pref.PreferredPriceRange = priceRange;

        await _context.SaveChangesAsync();
    }

    // ── Helper: parse "50000-200000" → (min, max) ─────────────────────────
    private static (decimal min, decimal max) ParsePriceRange(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (0, decimal.MaxValue);

        var parts = raw.Split('-');
        if (parts.Length == 2
            && decimal.TryParse(parts[0].Trim(), out decimal min)
            && decimal.TryParse(parts[1].Trim(), out decimal max))
            return (min, max);

        return (0, decimal.MaxValue);
    }
}
