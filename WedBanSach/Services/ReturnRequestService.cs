using WedBanSach.Models;
using WedBanSach.Repositories;

namespace WedBanSach.Services;

public class ReturnRequestService : IReturnRequestService
{
    private readonly IReturnRequestRepository _repository;
    private readonly EmailService _emailService;

    public ReturnRequestService(IReturnRequestRepository repository, EmailService emailService)
    {
        _repository = repository;
        _emailService = emailService;
    }

    public async Task<IEnumerable<ReturnRequest>> GetAllRequestsAsync()
    {
        return await _repository.GetAllRequestsAsync();
    }

    public async Task<IEnumerable<ReturnRequest>> GetRequestsByCustomerIdAsync(int customerId)
    {
        return await _repository.GetRequestsByCustomerIdAsync(customerId);
    }

    public async Task<ReturnRequest?> GetRequestByIdAsync(int id)
    {
        return await _repository.GetRequestByIdAsync(id);
    }

    public async Task<bool> VerifyOrderOwnershipAsync(int orderId, int customerId)
    {
        var order = await _repository.GetOrderForVerificationAsync(orderId, customerId);
        return order != null;
    }

    public async Task<bool> CreateRequestAsync(ReturnRequest request, List<string> imageUrls)
    {
        // 1. Verify ownership
        var order = await _repository.GetOrderForVerificationAsync(request.OrderId, request.CustomerId);
        if (order == null) return false;

        // 2. Set defaults
        request.Status = "Pending";
        request.CreatedAt = DateTime.Now;
        request.UpdatedAt = DateTime.Now;

        // 3. Save request
        await _repository.AddRequestAsync(request);
        
        // Save images
        foreach (var url in imageUrls)
        {
            var img = new ReturnRequestImage
            {
                ReturnRequest = request,
                ImageUrl = url
            };
            await _repository.AddRequestImageAsync(img);
        }

        var success = await _repository.SaveChangesAsync();
        
        if (success)
        {
            // Load customer details for email
            var customerName = request.Customer?.FullName ?? "Quý khách";
            var customerEmail = request.Customer?.Email;

            if (string.IsNullOrEmpty(customerEmail) && order.User != null)
            {
                customerEmail = order.User.Email;
                customerName = order.User.FullName;
            }

            if (!string.IsNullOrEmpty(customerEmail))
            {
                // Send registration confirmation email
                await _emailService.SendReturnRequestCreatedEmailAsync(customerEmail, customerName, request);
            }
        }

        return success;
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, string adminNote)
    {
        var request = await _repository.GetRequestByIdAsync(id);
        if (request == null) return false;

        var oldStatus = request.Status;
        request.Status = status;
        request.AdminNote = adminNote;
        request.UpdatedAt = DateTime.Now;

        // 1. If transitioning to Approved or Completed, perform automatic faulty stock updates
        if ((status == "Approved" || status == "Completed") && oldStatus != "Approved" && oldStatus != "Completed")
        {
            if (request.BookID.HasValue && request.Quantity > 0)
            {
                var book = await _repository.GetBookByIdAsync(request.BookID.Value);
                if (book != null)
                {
                    // Move returned product to faulty stock
                    book.FaultyQuantity += request.Quantity;

                    // Log returned faulty product to inventory logs
                    var faultyLog = new InventoryLog
                    {
                        BookID = book.BookID,
                        ChangeQuantity = request.Quantity,
                        Reason = $"Thu hồi sản phẩm lỗi từ yêu cầu #{request.Id} ({request.RequestType})",
                        CreatedAt = DateTime.Now
                    };
                    await _repository.AddInventoryLogAsync(faultyLog);

                    // If request type is Exchange, we deduct 1 from normal stock to send a new replacement to customer
                    if (request.RequestType == "Exchange")
                    {
                        book.StockQuantity = Math.Max(0, book.StockQuantity - request.Quantity);

                        var exchangeLog = new InventoryLog
                        {
                            BookID = book.BookID,
                            ChangeQuantity = -request.Quantity,
                            Reason = $"Xuất kho đổi mới sản phẩm cho khách từ yêu cầu #{request.Id}",
                            CreatedAt = DateTime.Now
                        };
                        await _repository.AddInventoryLogAsync(exchangeLog);
                    }
                }
            }
        }

        await _repository.UpdateRequestAsync(request);
        var success = await _repository.SaveChangesAsync();

        if (success)
        {
            var customerEmail = request.Customer?.Email;
            var customerName = request.Customer?.FullName ?? "Quý khách";

            if (!string.IsNullOrEmpty(customerEmail))
            {
                await _emailService.SendReturnRequestStatusUpdatedEmailAsync(customerEmail, customerName, request);
            }
        }

        return success;
    }

    public async Task<bool> ApproveRefundAsync(int id, decimal refundAmount, string method, string adminNote, string? transactionCode)
    {
        var request = await _repository.GetRequestByIdAsync(id);
        if (request == null) return false;

        // 1. Set refund details
        request.RefundAmount = refundAmount;
        request.Status = "Completed";
        request.AdminNote = adminNote;
        request.UpdatedAt = DateTime.Now;

        // 2. Perform faulty stock updates if not done yet
        if (request.BookID.HasValue && request.Quantity > 0)
        {
            var book = await _repository.GetBookByIdAsync(request.BookID.Value);
            if (book != null)
            {
                book.FaultyQuantity += request.Quantity;

                var faultyLog = new InventoryLog
                {
                    BookID = book.BookID,
                    ChangeQuantity = request.Quantity,
                    Reason = $"Thu hồi sản phẩm lỗi từ yêu cầu trả hàng hoàn tiền #{request.Id}",
                    CreatedAt = DateTime.Now
                };
                await _repository.AddInventoryLogAsync(faultyLog);
            }
        }

        // 3. Create Refund Transaction
        var transaction = new RefundTransaction
        {
            ReturnRequestId = request.Id,
            RefundMethod = method,
            RefundStatus = "Success",
            RefundDate = DateTime.Now,
            TransactionCode = transactionCode ?? $"REF-{DateTime.Now.Ticks}"
        };
        await _repository.AddRefundTransactionAsync(transaction);

        await _repository.UpdateRequestAsync(request);
        var success = await _repository.SaveChangesAsync();

        if (success)
        {
            var customerEmail = request.Customer?.Email;
            var customerName = request.Customer?.FullName ?? "Quý khách";

            if (!string.IsNullOrEmpty(customerEmail))
            {
                await _emailService.SendRefundCompletedEmailAsync(customerEmail, customerName, request, transaction);
            }
        }

        return success;
    }

    public async Task<bool> RejectRequestAsync(int id, string adminNote)
    {
        var request = await _repository.GetRequestByIdAsync(id);
        if (request == null) return false;

        request.Status = "Rejected";
        request.AdminNote = adminNote;
        request.UpdatedAt = DateTime.Now;

        await _repository.UpdateRequestAsync(request);
        var success = await _repository.SaveChangesAsync();

        if (success)
        {
            var customerEmail = request.Customer?.Email;
            var customerName = request.Customer?.FullName ?? "Quý khách";

            if (!string.IsNullOrEmpty(customerEmail))
            {
                await _emailService.SendReturnRequestStatusUpdatedEmailAsync(customerEmail, customerName, request);
            }
        }

        return success;
    }
}
