using WedBanSach.Models;

namespace WedBanSach.Repositories;

public interface IWarrantyRequestRepository
{
    Task<IEnumerable<WarrantyRequest>> GetAllRequestsAsync();
    Task<IEnumerable<WarrantyRequest>> GetRequestsByCustomerIdAsync(int customerId);
    Task<WarrantyRequest?> GetRequestByIdAsync(int id);
    Task AddRequestAsync(WarrantyRequest request);
    Task UpdateRequestAsync(WarrantyRequest request);
    Task<bool> SaveChangesAsync();
}
