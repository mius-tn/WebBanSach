using WedBanSach.Models;

namespace WedBanSach.Repositories;

public interface IReturnRequestRepository
{
    Task<IEnumerable<ReturnRequest>> GetAllRequestsAsync();
    Task<IEnumerable<ReturnRequest>> GetRequestsByCustomerIdAsync(int customerId);
    Task<ReturnRequest?> GetRequestByIdAsync(int id);
    Task<Order?> GetOrderForVerificationAsync(int orderId, int customerId);
    Task AddRequestAsync(ReturnRequest request);
    Task UpdateRequestAsync(ReturnRequest request);
    Task AddRequestImageAsync(ReturnRequestImage image);
    Task AddRefundTransactionAsync(RefundTransaction transaction);
    Task<Book?> GetBookByIdAsync(int bookId);
    Task AddInventoryLogAsync(InventoryLog log);
    Task<bool> SaveChangesAsync();
}
