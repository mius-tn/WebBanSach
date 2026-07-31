using WedBanSach.Models.Apriori;
using WedBanSach.ViewModels;

namespace WedBanSach.Services.Apriori;

public interface IAprioriService
{
    Task TrainModelAsync();
    Task<AprioriDashboardViewModel> GetDashboardDataAsync();
    Task<PaginatedResult<AprioriRuleViewModel>> GetRulesAsync(int page, int pageSize);
    Task<List<BookRecommendation>> GetRecommendationsForBookAsync(int bookId, int top = 5);
    Task<List<BookRecommendation>> GetRecommendationsForCartAsync(List<int> cartBookIds, int top = 5);
    Task<AprioriConfig> GetConfigAsync();
    Task UpdateConfigAsync(AprioriConfig config);
}
