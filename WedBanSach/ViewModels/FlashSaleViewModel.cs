using WedBanSach.Models;

namespace WedBanSach.ViewModels;

public class FlashSaleViewModel
{
    /// <summary>
    /// Chiến dịch đang được hiển thị (selected tab)
    /// </summary>
    public PromotionCampaign? SelectedCampaign { get; set; }

    /// <summary>
    /// Tất cả chiến dịch (để hiển thị tabs)
    /// </summary>
    public List<PromotionCampaign> AllCampaigns { get; set; } = new();

    /// <summary>
    /// Danh sách sách đã phân trang
    /// </summary>
    public List<Book> Books { get; set; } = new();

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // Filters
    public int? SelectedCampaignId { get; set; }
    public string? SortOrder { get; set; }
    public string? PriceRange { get; set; }
}
