using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Jobs
{
    public class CheckPromotionExpirationJob
    {
        private readonly BookStoreDbContext _context;
        private readonly ILogger<CheckPromotionExpirationJob> _logger;

        public CheckPromotionExpirationJob(BookStoreDbContext context, ILogger<CheckPromotionExpirationJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var now = DateTime.Now;

            _logger.LogInformation($"Running CheckPromotionExpirationJob at {now}");

            // ========================================
            // 1. Deactivate expired book-level promotions
            // ========================================
            var expiredPromotions = await _context.Books
                .Where(b => b.IsPromotionActive && b.SaleEndDate.HasValue && b.SaleEndDate.Value <= now)
                .ToListAsync();

            if (expiredPromotions.Any())
            {
                foreach (var book in expiredPromotions)
                {
                    var oldPrice = book.CurrentPrice;
                    book.IsPromotionActive = false;
                    book.CurrentPrice = book.OriginalPrice;
                    book.SalePrice = null;
                    book.SalePercent = null;

                    _context.PriceHistories.Add(new PriceHistory
                    {
                        BookID = book.BookID,
                        OldPrice = oldPrice,
                        NewPrice = book.OriginalPrice,
                        ChangeType = "PromotionEnd",
                        ChangedBy = "System",
                        ChangedAt = now,
                        Reason = "Khuyến mãi đã hết hạn (tự động)"
                    });
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Deactivated promotions for {expiredPromotions.Count} books.");
            }

            // ========================================
            // 2. Auto-activate book-level promotions that should start
            // ========================================
            var startingPromotions = await _context.Books
                .Where(b => !b.IsPromotionActive && b.SaleStartDate.HasValue && b.SaleStartDate.Value <= now &&
                            (!b.SaleEndDate.HasValue || b.SaleEndDate.Value > now) &&
                            b.SalePrice.HasValue)
                .ToListAsync();

            if (startingPromotions.Any())
            {
                foreach (var book in startingPromotions)
                {
                    var oldPrice = book.CurrentPrice;
                    book.IsPromotionActive = true;
                    book.CurrentPrice = book.SalePrice!.Value;

                    _context.PriceHistories.Add(new PriceHistory
                    {
                        BookID = book.BookID,
                        OldPrice = oldPrice,
                        NewPrice = book.SalePrice.Value,
                        ChangeType = "PromotionStart",
                        ChangedBy = "System",
                        ChangedAt = now,
                        Reason = "Bắt đầu khuyến mãi (tự động)"
                    });
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Activated promotions for {startingPromotions.Count} books.");
            }

            // ========================================
            // 3. Deactivate expired campaigns
            // ========================================
            var expiredCampaigns = await _context.PromotionCampaigns
                .Include(c => c.CampaignBooks)
                .Where(c => c.IsActive && c.EndDate <= now)
                .ToListAsync();

            foreach (var campaign in expiredCampaigns)
            {
                campaign.IsActive = false;

                var bookIds = campaign.CampaignBooks.Select(cb => cb.BookID).ToList();
                if (bookIds.Any())
                {
                    var books = await _context.Books
                        .Where(b => bookIds.Contains(b.BookID) && b.IsPromotionActive)
                        .ToListAsync();

                    foreach (var book in books)
                    {
                        var oldPrice = book.CurrentPrice;
                        book.IsPromotionActive = false;
                        book.CurrentPrice = book.OriginalPrice;
                        book.SalePrice = null;
                        book.SalePercent = null;
                        book.SaleStartDate = null;
                        book.SaleEndDate = null;

                        _context.PriceHistories.Add(new PriceHistory
                        {
                            BookID = book.BookID,
                            OldPrice = oldPrice,
                            NewPrice = book.OriginalPrice,
                            ChangeType = "PromotionEnd",
                            ChangedBy = "System",
                            ChangedAt = now,
                            Reason = $"Chiến dịch \"{campaign.Name}\" đã hết hạn (tự động)"
                        });
                    }

                    _logger.LogInformation($"Campaign \"{campaign.Name}\" expired. Deactivated promotions for {books.Count} books.");
                }
            }

            // ========================================
            // 4. Auto-activate campaigns that should start
            // ========================================
            var activeCampaigns = await _context.PromotionCampaigns
                .Include(c => c.CampaignBooks)
                    .ThenInclude(cb => cb.Book)
                .Where(c => c.IsActive && c.StartDate <= now && c.EndDate > now)
                .ToListAsync();

            bool hasChanges = false;
            foreach (var campaign in activeCampaigns)
            {
                var bookIds = campaign.CampaignBooks.Select(cb => cb.BookID).ToList();
                if (bookIds.Any())
                {
                    var booksToUpdate = campaign.CampaignBooks
                        .Select(cb => cb.Book)
                        .Where(b => !b.IsPromotionActive || b.SaleStartDate != campaign.StartDate || b.SaleEndDate != campaign.EndDate)
                        .ToList();

                    if (booksToUpdate.Any())
                    {
                        hasChanges = true;
                        foreach (var book in booksToUpdate)
                        {
                            var oldPrice = book.CurrentPrice;

                            decimal salePrice;
                            int salePercent;
                            if (campaign.DiscountType == "Percentage")
                            {
                                salePercent = (int)campaign.DiscountValue;
                                salePrice = book.OriginalPrice * (1 - campaign.DiscountValue / 100m);
                            }
                            else
                            {
                                salePrice = Math.Max(0, book.OriginalPrice - campaign.DiscountValue);
                                salePercent = book.OriginalPrice > 0
                                    ? (int)Math.Round((campaign.DiscountValue / book.OriginalPrice) * 100)
                                    : 0;
                            }

                            book.SalePercent = salePercent;
                            book.SalePrice = salePrice;
                            book.SaleStartDate = campaign.StartDate;
                            book.SaleEndDate = campaign.EndDate;
                            book.IsPromotionActive = true;
                            book.CurrentPrice = salePrice;

                            _context.PriceHistories.Add(new PriceHistory
                            {
                                BookID = book.BookID,
                                OldPrice = oldPrice,
                                NewPrice = salePrice,
                                ChangeType = "PromotionStart",
                                ChangedBy = "System",
                                ChangedAt = now,
                                Reason = $"Chiến dịch \"{campaign.Name}\" bắt đầu (tự động)"
                            });
                        }

                        _logger.LogInformation($"Campaign \"{campaign.Name}\" auto-applied. Activated promotions for {booksToUpdate.Count} books.");
                    }
                }
            }

            if (expiredCampaigns.Any() || hasChanges)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
