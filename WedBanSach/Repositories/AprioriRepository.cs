using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models.Apriori;

namespace WedBanSach.Repositories;

public class AprioriRepository : IAprioriRepository
{
    private readonly BookStoreDbContext _context;

    public AprioriRepository(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<List<AprioriRule>> GetActiveRulesAsync()
    {
        return await _context.AprioriRules.Where(r => r.IsActive).ToListAsync();
    }

    public async Task<List<AprioriRule>> GetRulesByBookIdAsync(int bookId)
    {
        string searchKey = bookId.ToString();
        // Since AntecedentKey is a comma-separated list of IDs, we need to match it carefully.
        // A simpler way for this project is using Contains, but it might match "12" when searching for "1".
        // For accuracy, we fetch rules and filter in memory if the dataset isn't huge.
        var allActive = await GetActiveRulesAsync();
        return allActive.Where(r => r.AntecedentKey.Split(',').Contains(searchKey)).ToList();
    }

    public async Task<List<AprioriRule>> GetRulesByBookIdsAsync(List<int> bookIds)
    {
        var strIds = bookIds.Select(id => id.ToString()).ToList();
        var allActive = await GetActiveRulesAsync();
        return allActive.Where(r => 
            r.AntecedentKey.Split(',').Intersect(strIds).Any()
        ).ToList();
    }

    public async Task<List<AprioriFrequentItemset>> GetFrequentItemsetsAsync(int? minSize, int? maxSize)
    {
        var query = _context.AprioriFrequentItemsets.AsQueryable();
        if (minSize.HasValue) query = query.Where(i => i.ItemsetSize >= minSize.Value);
        if (maxSize.HasValue) query = query.Where(i => i.ItemsetSize <= maxSize.Value);
        return await query.ToListAsync();
    }

    public async Task<AprioriConfig?> GetConfigAsync()
    {
        return await _context.AprioriConfigs.FirstOrDefaultAsync();
    }

    public async Task SaveConfigAsync(AprioriConfig config)
    {
        var existing = await _context.AprioriConfigs.FirstOrDefaultAsync();
        if (existing == null)
        {
            await _context.AprioriConfigs.AddAsync(config);
        }
        else
        {
            existing.MinSupport = config.MinSupport;
            existing.MinConfidence = config.MinConfidence;
            existing.MinLift = config.MinLift;
            existing.MaxItemsetSize = config.MaxItemsetSize;
            existing.MinTransactionCount = config.MinTransactionCount;
            existing.AutoRetrain = config.AutoRetrain;
            existing.TrainingIntervalHours = config.TrainingIntervalHours;
            existing.CacheTimeMinutes = config.CacheTimeMinutes;
        }
        await _context.SaveChangesAsync();
    }

    public async Task SaveTrainingResultsAsync(AprioriTrainingHistory history, List<AprioriFrequentItemset> itemsets, List<AprioriRule> rules)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.AprioriTrainingHistories.AddAsync(history);
            await _context.SaveChangesAsync(); // Get Session Id

            int sessionId = history.Id;

            // Update sessionId
            itemsets.ForEach(i => i.TrainingSessionId = sessionId);
            rules.ForEach(r => r.TrainingSessionId = sessionId);

            // Bulk insert (EF Core 9 has AddRange optimization)
            await _context.AprioriFrequentItemsets.AddRangeAsync(itemsets);
            await _context.AprioriRules.AddRangeAsync(rules);
            
            // Deactivate old rules
            var oldRules = await _context.AprioriRules.Where(r => r.TrainingSessionId != sessionId && r.IsActive).ToListAsync();
            oldRules.ForEach(r => r.IsActive = false);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<AprioriTrainingHistory>> GetTrainingHistoryAsync(int page, int pageSize)
    {
        return await _context.AprioriTrainingHistories
            .OrderByDescending(h => h.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    
    public async Task<int> GetTrainingHistoryCountAsync()
    {
        return await _context.AprioriTrainingHistories.CountAsync();
    }

    public async Task<List<AprioriRecommendation>> GetRecommendationsAsync(int bookId, int top)
    {
        var now = DateTime.Now;
        return await _context.AprioriRecommendations
            .Where(r => r.SourceBookId == bookId && (r.ExpiresAt == null || r.ExpiresAt > now))
            .OrderByDescending(r => r.Score)
            .Take(top)
            .ToListAsync();
    }

    public async Task SaveRecommendationsAsync(List<AprioriRecommendation> recommendations)
    {
        await _context.AprioriRecommendations.AddRangeAsync(recommendations);
        await _context.SaveChangesAsync();
    }

    public async Task ClearOldDataAsync(int? keepSessionId)
    {
        if (keepSessionId.HasValue)
        {
            var oldItemsets = _context.AprioriFrequentItemsets.Where(i => i.TrainingSessionId != keepSessionId.Value);
            _context.AprioriFrequentItemsets.RemoveRange(oldItemsets);
            
            var oldRules = _context.AprioriRules.Where(r => r.TrainingSessionId != keepSessionId.Value);
            _context.AprioriRules.RemoveRange(oldRules);
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddLogAsync(AprioriLog log)
    {
        await _context.AprioriLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AprioriLog>> GetLogsAsync(int page, int pageSize)
    {
        return await _context.AprioriLogs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetLogsCountAsync()
    {
        return await _context.AprioriLogs.CountAsync();
    }

    public async Task<int> GetTotalTransactionsAsync()
    {
        var lastTraining = await GetLatestTrainingHistoryAsync();
        return lastTraining?.TotalTransactions ?? 0;
    }

    public async Task<int> GetTotalRulesAsync()
    {
        return await _context.AprioriRules.CountAsync(r => r.IsActive);
    }

    public async Task<int> GetTotalFrequentItemsetsAsync()
    {
        var lastTraining = await GetLatestTrainingHistoryAsync();
        if (lastTraining == null) return 0;
        return await _context.AprioriFrequentItemsets.CountAsync(i => i.TrainingSessionId == lastTraining.Id);
    }

    public async Task<Dictionary<string, int>> GetTopItemsAsync(int top)
    {
        // Simplification for dashboard
        return new Dictionary<string, int>();
    }

    public async Task<AprioriTrainingHistory?> GetLatestTrainingHistoryAsync()
    {
        return await _context.AprioriTrainingHistories
            .OrderByDescending(h => h.StartTime)
            .FirstOrDefaultAsync();
    }
}
