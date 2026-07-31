using WedBanSach.Models;
using WedBanSach.Models.Apriori;

namespace WedBanSach.ViewModels;

public class AprioriDashboardViewModel
{
    public int TotalTransactions { get; set; }
    public int TotalRules { get; set; }
    public int TotalFrequentItemsets { get; set; }
    public decimal AvgConfidence { get; set; }
    public decimal AvgLift { get; set; }
    public Dictionary<string, int> TopItems { get; set; } = new();
    public Dictionary<string, int> TopCategories { get; set; } = new();
    public AprioriTrainingHistory? LastTraining { get; set; }
}

public class AprioriRuleViewModel
{
    public int Id { get; set; }
    public string AntecedentNames { get; set; } = string.Empty;
    public string ConsequentNames { get; set; } = string.Empty;
    public decimal Support { get; set; }
    public decimal Confidence { get; set; }
    public decimal Lift { get; set; }
    public decimal Conviction { get; set; }
    public decimal Leverage { get; set; }
    public decimal JaccardSimilarity { get; set; }
    public decimal RecommendationScore { get; set; }
    public bool IsActive { get; set; }
}

public class FrequentItemsetViewModel
{
    public int Id { get; set; }
    public string ItemNames { get; set; } = string.Empty;
    public int ItemsetSize { get; set; }
    public decimal Support { get; set; }
    public int TransactionCount { get; set; }
}

public class BookRecommendation
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal Score { get; set; }
    public string RecommendationType { get; set; } = string.Empty;
}

public class ComboSuggestion
{
    public List<Book> Books { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal Savings { get; set; }
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
