using WedBanSach.Models.Apriori;

namespace WedBanSach.Repositories;

public interface IAprioriRepository
{
    Task<List<AprioriRule>> GetActiveRulesAsync();
    Task<List<AprioriRule>> GetRulesByBookIdAsync(int bookId);
    Task<List<AprioriRule>> GetRulesByBookIdsAsync(List<int> bookIds);
    Task<List<AprioriFrequentItemset>> GetFrequentItemsetsAsync(int? minSize, int? maxSize);
    Task<AprioriConfig?> GetConfigAsync();
    Task SaveConfigAsync(AprioriConfig config);
    Task SaveTrainingResultsAsync(AprioriTrainingHistory history, List<AprioriFrequentItemset> itemsets, List<AprioriRule> rules);
    Task<List<AprioriTrainingHistory>> GetTrainingHistoryAsync(int page, int pageSize);
    Task<int> GetTrainingHistoryCountAsync();
    Task<List<AprioriRecommendation>> GetRecommendationsAsync(int bookId, int top);
    Task SaveRecommendationsAsync(List<AprioriRecommendation> recommendations);
    Task ClearOldDataAsync(int? keepSessionId);
    Task AddLogAsync(AprioriLog log);
    Task<List<AprioriLog>> GetLogsAsync(int page, int pageSize);
    Task<int> GetLogsCountAsync();
    
    // Dashboard data
    Task<int> GetTotalTransactionsAsync();
    Task<int> GetTotalRulesAsync();
    Task<int> GetTotalFrequentItemsetsAsync();
    Task<Dictionary<string, int>> GetTopItemsAsync(int top);
    Task<AprioriTrainingHistory?> GetLatestTrainingHistoryAsync();
}
