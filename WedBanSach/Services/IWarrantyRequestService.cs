using WedBanSach.Models;

namespace WedBanSach.Services;

public interface IWarrantyRequestService
{
    Task<IEnumerable<WarrantyRequest>> GetAllRequestsAsync();
    Task<IEnumerable<WarrantyRequest>> GetRequestsByCustomerIdAsync(int customerId);
    Task<WarrantyRequest?> GetRequestByIdAsync(int id);
    Task<bool> CreateRequestAsync(WarrantyRequest request);
    Task<bool> UpdateStatusAsync(int id, string status);
}
