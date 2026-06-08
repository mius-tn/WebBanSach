namespace WedBanSach.ViewModels;

public class DashboardStatsViewModel
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int CompletedRequests { get; set; }
    public decimal TotalRefunded { get; set; }
    
    // Status metrics
    public int TotalWarrantyRequests { get; set; }
    public int PendingWarrantyRequests { get; set; }

    // Charting data
    public List<string> MonthlyLabels { get; set; } = new();
    public List<decimal> MonthlyRefundAmounts { get; set; } = new();
    public List<int> MonthlyRequestCounts { get; set; } = new();
}
