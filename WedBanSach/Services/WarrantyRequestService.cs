using WedBanSach.Models;
using WedBanSach.Repositories;

namespace WedBanSach.Services;

public class WarrantyRequestService : IWarrantyRequestService
{
    private readonly IWarrantyRequestRepository _repository;
    private readonly EmailService _emailService;

    public WarrantyRequestService(IWarrantyRequestRepository repository, EmailService emailService)
    {
        _repository = repository;
        _emailService = emailService;
    }

    public async Task<IEnumerable<WarrantyRequest>> GetAllRequestsAsync()
    {
        return await _repository.GetAllRequestsAsync();
    }

    public async Task<IEnumerable<WarrantyRequest>> GetRequestsByCustomerIdAsync(int customerId)
    {
        return await _repository.GetRequestsByCustomerIdAsync(customerId);
    }

    public async Task<WarrantyRequest?> GetRequestByIdAsync(int id)
    {
        return await _repository.GetRequestByIdAsync(id);
    }

    public async Task<bool> CreateRequestAsync(WarrantyRequest request)
    {
        request.Status = "Pending";
        request.CreatedAt = DateTime.Now;

        await _repository.AddRequestAsync(request);
        var success = await _repository.SaveChangesAsync();

        if (success)
        {
            var customerEmail = request.Customer?.Email;
            var customerName = request.Customer?.FullName ?? "Quý khách";

            // If Customer object is not loaded, we try to load it or handle it gracefully
            if (!string.IsNullOrEmpty(customerEmail))
            {
                var subject = $"Đăng ký bảo hành thành công - WedBanSach";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #e91e63;'>Xin chào {customerName}!</h2>
                            <p>Chúng tôi đã nhận được yêu cầu bảo hành cho sản phẩm <strong>{(request.Product?.Title ?? "Phụ kiện/Sách")}</strong>.</p>
                            <p><strong>Nội dung lỗi kỹ thuật:</strong> {request.IssueDescription}</p>
                            <p>Mã yêu cầu bảo hành của bạn là: <strong>#WR-{request.Id}</strong>.</p>
                            <p>Đội ngũ CSKH sẽ kiểm tra thông tin và liên hệ lại với bạn trong vòng 24 giờ làm việc để hướng dẫn gửi sản phẩm bảo hành.</p>
                            <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>
                            <p style='color: #999; font-size: 12px;'>Cảm ơn bạn đã đồng hành cùng WedBanSach!</p>
                        </div>
                    </body>
                    </html>";
                await _emailService.SendEmailGenericAsync(customerEmail, subject, body);
            }
        }

        return success;
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        var request = await _repository.GetRequestByIdAsync(id);
        if (request == null) return false;

        request.Status = status;
        await _repository.UpdateRequestAsync(request);
        var success = await _repository.SaveChangesAsync();

        if (success)
        {
            var customerEmail = request.Customer?.Email;
            var customerName = request.Customer?.FullName ?? "Quý khách";

            if (!string.IsNullOrEmpty(customerEmail))
            {
                var subject = $"Cập nhật trạng thái yêu cầu bảo hành #{request.Id} - WedBanSach";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #e91e63;'>Xin chào {customerName}!</h2>
                            <p>Yêu cầu bảo hành mã số <strong>#WR-{request.Id}</strong> của bạn đã được cập nhật trạng thái mới:</p>
                            <div style='background-color: #fff5f8; padding: 15px; border-radius: 8px; border: 1px solid #f8cdda; margin: 20px 0;'>
                                <h3 style='margin: 0; color: #e91e63;'>Trạng thái: {TranslateStatus(status)}</h3>
                            </div>
                            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với bộ phận chăm sóc khách hàng của chúng tôi.</p>
                            <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>
                            <p style='color: #999; font-size: 12px;'>Cảm ơn bạn đã đồng hành cùng WedBanSach!</p>
                        </div>
                    </body>
                    </html>";
                await _emailService.SendEmailGenericAsync(customerEmail, subject, body);
            }
        }

        return success;
    }

    private string TranslateStatus(string status)
    {
        return status switch
        {
            "Pending" => "Đang chờ xử lý",
            "Approved" => "Đã chấp nhận bảo hành",
            "Rejected" => "Từ chối bảo hành",
            "Completed" => "Đã hoàn thành bảo hành (Đã gửi lại sản phẩm/Đổi mới)",
            _ => status
        };
    }
}
