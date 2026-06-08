using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Repositories;

public class ReturnRequestRepository : IReturnRequestRepository
{
    private readonly BookStoreDbContext _context;

    public ReturnRequestRepository(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReturnRequest>> GetAllRequestsAsync()
    {
        return await _context.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.Customer)
            .Include(r => r.Images)
            .Include(r => r.RefundTransactions)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ReturnRequest>> GetRequestsByCustomerIdAsync(int customerId)
    {
        return await _context.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.Images)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<ReturnRequest?> GetRequestByIdAsync(int id)
    {
        return await _context.ReturnRequests
            .Include(r => r.Order)
                .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
            .Include(r => r.Customer)
            .Include(r => r.Images)
            .Include(r => r.RefundTransactions)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Order?> GetOrderForVerificationAsync(int orderId, int customerId)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
            .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == customerId);
    }

    public async Task AddRequestAsync(ReturnRequest request)
    {
        await _context.ReturnRequests.AddAsync(request);
    }

    public async Task UpdateRequestAsync(ReturnRequest request)
    {
        request.UpdatedAt = DateTime.Now;
        _context.ReturnRequests.Update(request);
        await Task.CompletedTask;
    }

    public async Task AddRequestImageAsync(ReturnRequestImage image)
    {
        await _context.ReturnRequestImages.AddAsync(image);
    }

    public async Task AddRefundTransactionAsync(RefundTransaction transaction)
    {
        await _context.RefundTransactions.AddAsync(transaction);
    }

    public async Task<Book?> GetBookByIdAsync(int bookId)
    {
        return await _context.Books.FindAsync(bookId);
    }

    public async Task AddInventoryLogAsync(InventoryLog log)
    {
        await _context.InventoryLogs.AddAsync(log);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
