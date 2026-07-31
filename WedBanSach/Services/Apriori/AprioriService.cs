using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models.Apriori;
using WedBanSach.Repositories;
using WedBanSach.ViewModels;

namespace WedBanSach.Services.Apriori;

public class AprioriService : IAprioriService
{
    private readonly IAprioriRepository _repository;
    private readonly BookStoreDbContext _dbContext;

    public AprioriService(IAprioriRepository repository, BookStoreDbContext dbContext)
    {
        _repository = repository;
        _dbContext = dbContext;
    }

    public async Task TrainModelAsync()
    {
        var config = await _repository.GetConfigAsync();
        if (config == null) return;

        var history = new AprioriTrainingHistory
        {
            StartTime = DateTime.Now,
            Status = "Running",
            MinSupportUsed = (decimal)config.MinSupport,
            MinConfidenceUsed = (decimal)config.MinConfidence,
            MinLiftUsed = (decimal)config.MinLift,
            CreatedBy = "System"
        };

        try
        {
            // 1. Fetch transactions (Chỉ lấy các đơn hàng đã giao thành công)
            var transactions = await _dbContext.Orders
                .Where(o => o.OrderStatus == "Delivered" || o.OrderStatus == "Completed")
                .Include(o => o.OrderDetails)
                .Select(o => o.OrderDetails.Select(od => od.BookID).ToList())
                .ToListAsync();

            if (transactions.Count < 1)
            {
                throw new Exception($"Không có đơn hàng nào trong hệ thống để phân tích.");
            }

            var engine = new AprioriEngine(config.MinSupport, config.MinConfidence, config.MinLift, config.MaxItemsetSize);
            engine.LoadTransactions(transactions);

            // 2. Find frequent itemsets
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var frequentSets = engine.FindFrequentItemsets();
            
            // 3. Generate rules
            var rawRules = engine.GenerateRules();
            watch.Stop();

            // Map to entities
            var itemsetEntities = frequentSets.Select(kvp => new AprioriFrequentItemset
            {
                ItemsetKey = kvp.Key,
                ItemsetSize = kvp.Key.Split(',').Length,
                Support = (decimal)kvp.Value / transactions.Count,
                TransactionCount = kvp.Value
            }).ToList();

            var ruleEntities = rawRules.Select(r => new AprioriRule
            {
                AntecedentKey = r.Antecedent,
                ConsequentKey = r.Consequent,
                Support = (decimal)r.Support,
                Confidence = (decimal)r.Confidence,
                Lift = (decimal)r.Lift,
                Conviction = (decimal)r.Conviction,
                Leverage = (decimal)r.Leverage,
                JaccardSimilarity = (decimal)r.JaccardSimilarity,
                CosineSimilarity = (decimal)r.CosineSimilarity,
                Kulczynski = (decimal)r.Kulczynski,
                AllConfidence = (decimal)r.AllConfidence,
                MaxConfidence = (decimal)r.MaxConfidence,
                RecommendationScore = (decimal)r.RecommendationScore,
                IsActive = true
            }).ToList();

            history.EndTime = DateTime.Now;
            history.Status = "Completed";
            history.TotalTransactions = transactions.Count;
            history.TotalFrequentItemsets = itemsetEntities.Count;
            history.TotalRules = ruleEntities.Count;
            history.DurationMs = watch.ElapsedMilliseconds;

            await _repository.SaveTrainingResultsAsync(history, itemsetEntities, ruleEntities);

            // Pre-calculate recommendations for faster access
            await PrecalculateRecommendationsAsync(ruleEntities);
            
            await _repository.AddLogAsync(new AprioriLog { Level = "Info", Message = "Training completed successfully.", TrainingSessionId = history.Id });
        }
        catch (Exception ex)
        {
            history.EndTime = DateTime.Now;
            history.Status = "Failed";
            history.ErrorMessage = ex.Message;
            await _repository.AddLogAsync(new AprioriLog { Level = "Error", Message = "Training failed", Details = ex.StackTrace });
        }
    }

    private async Task PrecalculateRecommendationsAsync(List<AprioriRule> rules)
    {
        // For simple 1-to-1 rules
        var recommendations = new List<AprioriRecommendation>();
        foreach (var rule in rules)
        {
            var sourceIds = rule.AntecedentKey.Split(',').Select(int.Parse).ToList();
            var targetIds = rule.ConsequentKey.Split(',').Select(int.Parse).ToList();

            if (sourceIds.Count == 1 && targetIds.Count == 1)
            {
                recommendations.Add(new AprioriRecommendation
                {
                    SourceBookId = sourceIds[0],
                    RecommendedBookId = targetIds[0],
                    Score = rule.RecommendationScore,
                    RuleId = rule.Id
                });
            }
        }
        
        // Remove old recommendations and insert new ones
        _dbContext.AprioriRecommendations.RemoveRange(_dbContext.AprioriRecommendations);
        await _repository.SaveRecommendationsAsync(recommendations);
    }

    public async Task<AprioriDashboardViewModel> GetDashboardDataAsync()
    {
        var model = new AprioriDashboardViewModel
        {
            TotalTransactions = await _repository.GetTotalTransactionsAsync(),
            TotalRules = await _repository.GetTotalRulesAsync(),
            TotalFrequentItemsets = await _repository.GetTotalFrequentItemsetsAsync(),
            LastTraining = await _repository.GetLatestTrainingHistoryAsync()
        };

        var rules = await _repository.GetActiveRulesAsync();
        if (rules.Any())
        {
            model.AvgConfidence = rules.Average(r => r.Confidence);
            model.AvgLift = rules.Average(r => r.Lift);
        }

        return model;
    }

    public async Task<PaginatedResult<AprioriRuleViewModel>> GetRulesAsync(int page, int pageSize)
    {
        var rules = await _repository.GetActiveRulesAsync();
        
        var pagedRules = rules.OrderByDescending(r => r.RecommendationScore)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToList();

        // Get book names
        var allBookIds = pagedRules.SelectMany(r => r.AntecedentKey.Split(',').Concat(r.ConsequentKey.Split(',')))
                                   .Select(int.Parse).Distinct().ToList();
        
        var bookNames = await _dbContext.Books
            .Where(b => allBookIds.Contains(b.BookID))
            .ToDictionaryAsync(b => b.BookID, b => b.Title);

        var viewModels = pagedRules.Select(r => new AprioriRuleViewModel
        {
            Id = r.Id,
            AntecedentNames = string.Join(", ", r.AntecedentKey.Split(',').Select(id => bookNames.GetValueOrDefault(int.Parse(id), "Unknown"))),
            ConsequentNames = string.Join(", ", r.ConsequentKey.Split(',').Select(id => bookNames.GetValueOrDefault(int.Parse(id), "Unknown"))),
            Support = r.Support,
            Confidence = r.Confidence,
            Lift = r.Lift,
            RecommendationScore = r.RecommendationScore,
            IsActive = r.IsActive
        }).ToList();

        return new PaginatedResult<AprioriRuleViewModel>
        {
            Items = viewModels,
            TotalCount = rules.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<BookRecommendation>> GetRecommendationsForBookAsync(int bookId, int top = 5)
    {
        var recs = await _repository.GetRecommendationsAsync(bookId, top);
        var recommendedBookIds = recs.Select(r => r.RecommendedBookId).ToList();

        var books = await _dbContext.Books
            .Include(b => b.BookImages)
            .Where(b => recommendedBookIds.Contains(b.BookID) && b.Status == "Active" && (b.TotalStock - b.ReservedStock) > 0)
            .Select(b => new BookRecommendation
            {
                BookId = b.BookID,
                Title = b.Title,
                Price = b.CurrentPrice,
                SalePrice = b.SalePrice,
                ImageUrl = b.BookImages.FirstOrDefault(i => i.IsMain).ImageUrl,
                RecommendationType = "FrequentlyBoughtTogether"
            })
            .ToListAsync();

        foreach (var book in books)
        {
            book.Score = recs.FirstOrDefault(r => r.RecommendedBookId == book.BookId)?.Score ?? 0;
        }

        return books.OrderByDescending(b => b.Score).ToList();
    }

    public async Task<List<BookRecommendation>> GetRecommendationsForCartAsync(List<int> cartBookIds, int top = 5)
    {
        if (!cartBookIds.Any()) return new List<BookRecommendation>();

        var rules = await _repository.GetRulesByBookIdsAsync(cartBookIds);
        
        // Find rules where the antecedent is a subset of the cart
        var validRules = rules.Where(r => 
        {
            var antIds = r.AntecedentKey.Split(',').Select(int.Parse).ToList();
            return !antIds.Except(cartBookIds).Any();
        }).ToList();

        var recommendedBookIds = validRules
            .SelectMany(r => r.ConsequentKey.Split(',').Select(int.Parse))
            .Except(cartBookIds) // Don't recommend what's already in the cart
            .Distinct()
            .ToList();

        var books = await _dbContext.Books
            .Include(b => b.BookImages)
            .Where(b => recommendedBookIds.Contains(b.BookID) && b.Status == "Active" && (b.TotalStock - b.ReservedStock) > 0)
            .Take(top)
            .Select(b => new BookRecommendation
            {
                BookId = b.BookID,
                Title = b.Title,
                Price = b.CurrentPrice,
                SalePrice = b.SalePrice,
                ImageUrl = b.BookImages.FirstOrDefault(i => i.IsMain).ImageUrl,
                RecommendationType = "YouMightAlsoNeed"
            })
            .ToListAsync();

        return books;
    }

    public async Task<AprioriConfig> GetConfigAsync()
    {
        var config = await _repository.GetConfigAsync();
        if (config == null)
        {
            config = new AprioriConfig();
            await _repository.SaveConfigAsync(config);
        }
        return config;
    }

    public async Task UpdateConfigAsync(AprioriConfig config)
    {
        await _repository.SaveConfigAsync(config);
    }
}
