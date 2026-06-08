using WedBanSach.Models;

namespace WedBanSach.Services;

public interface IReturnRequestService
{
    Task<IEnumerable<ReturnRequest>> GetAllRequestsAsync();
    Task<IEnumerable<ReturnRequest>> GetRequestsByCustomerIdAsync(int customerId);
    Task<ReturnRequest?> GetRequestByIdAsync(int id);
    Task<bool> VerifyOrderOwnershipAsync(int orderId, int customerId);
    Task<bool> CreateRequestAsync(ReturnRequest request, List<string> imageUrls);
    Task<bool> UpdateStatusAsync(int id, string status, string adminNote);
    Task<bool> ApproveRefundAsync(int id, decimal refundAmount, string method, string adminNote, string? transactionCode);
    Task<bool> RejectRequestAsync(int id, string adminNote);
}
