using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Repositories;

public class WarrantyRequestRepository : IWarrantyRequestRepository
{
    private readonly BookStoreDbContext _context;

    public WarrantyRequestRepository(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WarrantyRequest>> GetAllRequestsAsync()
    {
        return await _context.WarrantyRequests
            .Include(w => w.Product)
            .Include(w => w.Customer)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<WarrantyRequest>> GetRequestsByCustomerIdAsync(int customerId)
    {
        return await _context.WarrantyRequests
            .Include(w => w.Product)
            .Where(w => w.CustomerId == customerId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<WarrantyRequest?> GetRequestByIdAsync(int id)
    {
        return await _context.WarrantyRequests
            .Include(w => w.Product)
            .Include(w => w.Customer)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task AddRequestAsync(WarrantyRequest request)
    {
        await _context.WarrantyRequests.AddAsync(request);
    }

    public async Task UpdateRequestAsync(WarrantyRequest request)
    {
        _context.WarrantyRequests.Update(request);
        await Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
