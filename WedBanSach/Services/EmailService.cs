using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using WedBanSach.Models;

namespace WedBanSach.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderPassword;
        private readonly string _senderName;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
            _senderPassword = _configuration["EmailSettings:SenderPassword"] ?? "";
            _senderName = _configuration["EmailSettings:SenderName"] ?? "WedBanSach";
        }

        public async Task<bool> SendVerificationEmailAsync(string toEmail, string userName, string verificationToken)
        {
            try
            {
                var verificationLink = $"https://localhost:5001/Account/VerifyEmail?token={verificationToken}";
                
                var subject = "Xác nhận địa chỉ email - WedBanSach";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #C92127;'>Xin chào {userName}!</h2>
                            <p>Cảm ơn bạn đã đăng ký tài khoản tại WedBanSach.</p>
                            <p>Vui lòng click vào nút bên dưới để xác nhận địa chỉ email của bạn:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{verificationLink}' 
                                   style='background-color: #C92127; color: white; padding: 12px 30px; 
                                          text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    Xác nhận Email
                                </a>
                            </div>
                            <p style='color: #666; font-size: 14px;'>
                                Hoặc copy link sau vào trình duyệt:<br>
                                <a href='{verificationLink}'>{verificationLink}</a>
                            </p>
                            <p style='color: #666; font-size: 14px;'>
                                Link này sẽ hết hạn sau 24 giờ.
                            </p>
                            <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>
                            <p style='color: #999; font-size: 12px;'>
                                Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.
                            </p>
                        </div>
                    </body>
                    </html>
                ";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending verification email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPhoneOTPEmailAsync(string toEmail, string userName, string otpCode)
        {
            try
            {
                var subject = "Mã OTP xác nhận số điện thoại - WedBanSach";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #C92127;'>Xin chào {userName}!</h2>
                            <p>Mã OTP để xác nhận email của bạn là:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px; display: inline-block;'>
                                    <h1 style='margin: 0; color: #C92127; font-size: 36px; letter-spacing: 5px;'>{otpCode}</h1>
                                </div>
                            </div>
                            <p style='color: #666; font-size: 14px;'>
                                Mã này sẽ hết hạn sau 5 phút.
                            </p>
                            <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>
                            <p style='color: #999; font-size: 12px;'>
                                Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.
                            </p>
                        </div>
                    </body>
                    </html>
                ";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending OTP email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendForgotPasswordEmailAsync(string toEmail, string userName, string otpCode)
        {
            try
            {
                var subject = "Đặt lại mật khẩu - WedBanSach";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #C92127;'>Xin chào {userName}!</h2>
                            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
                            <p>Mã xác thực (OTP) của bạn là:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px; display: inline-block;'>
                                    <h1 style='margin: 0; color: #C92127; font-size: 36px; letter-spacing: 5px;'>{otpCode}</h1>
                                </div>
                            </div>
                            <p style='color: #666; font-size: 14px;'>
                                Mã này sẽ hết hạn sau 5 phút.
                            </p>
                            <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>
                            <p style='color: #999; font-size: 12px;'>
                                Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này. Tài khoản của bạn vẫn an toàn.
                            </p>
                        </div>
                    </body>
                    </html>
                ";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending Forgot Password email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(WedBanSach.Models.Order order)
        {
            try
            {
                var subject = $"Thông tin đơn hàng #{order.OrderID} - WedBanSach";
                
                // Construct styles
                var headerStyle = "font-size: 20px; font-weight: bold; margin-bottom: 5px;";
                var badgeStyle = "background-color: #28a745; color: white; padding: 5px 15px; border-radius: 20px; font-size: 14px; font-weight: bold; display: inline-block;";
                var sectionHeader = "font-weight: bold; font-size: 14px; color: #666; margin-top: 20px; margin-bottom: 10px; text-transform: uppercase;";
                var tableStyle = "width: 100%; border-collapse: collapse; margin-top: 15px;";
                var thStyle = "text-align: left; padding: 10px; border-bottom: 1px solid #eee; color: #444; font-size: 13px;";
                var tdStyle = "padding: 15px 10px; border-bottom: 1px solid #eee; vertical-align: middle;";
                var priceStyle = "font-weight: bold; color: #333;";
                var totalStyle = "font-size: 18px; font-weight: bold; color: #C92127;";
                
                // Parse Shipping Address (Name | Phone | Address) if standard format (contains '|')
                var receiverName = order.User?.FullName;
                var receiverPhone = order.User?.Phone;
                var receiverAddress = order.ShippingAddress;

                if (order.ShippingAddress != null && order.ShippingAddress.Contains('|'))
                {
                    var parts = order.ShippingAddress.Split('|');
                    if (parts.Length > 0) receiverName = parts[0].Trim();
                    if (parts.Length > 1) receiverPhone = parts[1].Trim();
                    if (parts.Length > 2) receiverAddress = parts[2].Trim();
                }

                var sb = new System.Text.StringBuilder();
                sb.Append($@"
                    <html>
                    <body style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
                        <div style='max-width: 700px; margin: 0 auto; padding: 20px; background: #fff;'>
                            <!-- Header -->
                            <div style='display: flex; justify-content: space-between; align-items: start; margin-bottom: 30px;'>
                                <div>
                                    <h2 style='{headerStyle}'>Thông tin đơn hàng #{order.OrderID}</h2>
                                    <div style='{badgeStyle}'>Đã xác nhận</div> <!-- Assuming confirmed state -->
                                </div>
                            </div>

                            <!-- Receiver Info -->
                            <div style='margin-bottom: 30px;'>
                                <div style='{sectionHeader}'>NGƯỜI NHẬN</div>
                                <div style='font-size: 15px;'>
                                    <strong>{receiverName}</strong><br>
                                    {receiverAddress}<br>
                                    {receiverPhone}
                                </div>
                            </div>

                            <!-- Products Table -->
                            <table style='{tableStyle}'>
                                <thead>
                                    <tr>
                                        <th style='{thStyle}'>Sản phẩm</th>
                                        <th style='{thStyle} text-align: center;'>SL</th>
                                        <th style='{thStyle} text-align: right;'>Đơn giá</th>
                                        <th style='{thStyle} text-align: right;'>Thành tiền</th>
                                    </tr>
                                </thead>
                                <tbody>");

                // Loop Items
                if (order.OrderDetails != null)
                {
                    foreach (var item in order.OrderDetails)
                    {
                        // 1. Get Real Image URL
                        var imgUrl = item.Book?.BookImages?.FirstOrDefault(i => i.IsMain)?.ImageUrl;
                        
                        // 2. Default if not found
                        if (string.IsNullOrEmpty(imgUrl)) 
                        {
                            imgUrl = "https://cdn-icons-png.flaticon.com/512/2232/2232688.png";
                        }
                        // 3. If local path, prepend domain
                        else if (!imgUrl.StartsWith("http")) 
                        {
                            imgUrl = "https://localhost:7255" + imgUrl;
                        }
                        
                        sb.Append($@"
                                    <tr>
                                        <td style='{tdStyle}'>
                                            <div style='display: flex; align-items: center;'>
                                                <img src='{imgUrl}' 
                                                     onerror=""this.onerror=null;this.src='https://cdn-icons-png.flaticon.com/512/2232/2232688.png';""
                                                     width='50' height='70' style='object-fit: cover; border-radius: 4px; border: 1px solid #eee; margin-right: 15px;'>
                                                <span style='font-weight: 500;'>{item.Book?.Title}</span>
                                            </div>
                                        </td>
                                        <td style='{tdStyle} text-align: center;'>x {item.Quantity}</td>
                                        <td style='{tdStyle} text-align: right;'>{(item.UnitPrice ?? 0):N0} </td>
                                        <td style='{tdStyle} text-align: right; font-weight: bold;'>{(item.Quantity * (item.UnitPrice ?? 0)):N0} </td>
                                    </tr>");
                    }
                }

                sb.Append($@"
                                </tbody>
                            </table>

                            <!-- Footer / Totals -->
                            <div style='margin-top: 20px; border-top: 1px solid #eee; padding-top: 20px;'>
                                <div style='display: flex; justify-content: space-between; margin-bottom: 10px;'>
                                    <span style='color: #666;'>Phương thức thanh toán</span>
                                    <strong>{(order.PaymentMethod == "Bank Transfer" ? "Chuyển khoản" : order.PaymentMethod == "COD" ? "Thanh toán khi nhận hàng" : order.PaymentMethod)}</strong>
                                </div>
                                <div style='text-align: right;'>
                                    <p style='margin: 5px 0;'>Phí vận chuyển: {(order.ShippingFee ?? 0):N0} </p>
                                    <p style='margin: 10px 0; {totalStyle}'>Tổng cộng: {(order.TotalAmount ?? 0):N0} </p>
                                </div>
                            </div>
                            
                            <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>
                            <div style='text-align: center; color: #999; font-size: 13px;'>
                                <p>Cảm ơn bạn đã mua sắm tại WedBanSach!</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ");

                var userEmail = order.User?.Email;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    return await SendEmailAsync(userEmail, subject, sb.ToString());
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending order email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailGenericAsync(string toEmail, string subject, string body)
        {
            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendReturnRequestCreatedEmailAsync(string toEmail, string userName, ReturnRequest request)
        {
            var subject = $"Đăng ký yêu cầu đổi/trả/hoàn tiền #{request.Id} thành công - WedBanSach";
            var typeText = request.RequestType switch
            {
                "Exchange" => "Đổi hàng",
                "Return" => "Trả hàng",
                "Refund" => "Hoàn tiền",
                "Warranty" => "Bảo hành",
                _ => request.RequestType
            };
            
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #fce4ec; border-radius: 10px; background-color: #fff;'>
                        <h2 style='color: #e91e63; text-align: center; border-bottom: 2px solid #fce4ec; padding-bottom: 10px;'>Đăng Ký Đổi/Trả Thành Công</h2>
                        <p>Xin chào <strong>{userName}</strong>,</p>
                        <p>Chúng tôi xác nhận đã nhận được yêu cầu <strong>{typeText}</strong> từ bạn cho đơn hàng <strong>#{request.OrderId}</strong>.</p>
                        <div style='background-color: #fff5f8; padding: 15px; border-radius: 8px; border: 1px solid #f8cdda; margin: 20px 0;'>
                            <p style='margin: 5px 0;'><strong>Mã yêu cầu:</strong> #RQ-{request.Id}</p>
                            <p style='margin: 5px 0;'><strong>Loại yêu cầu:</strong> {typeText}</p>
                            <p style='margin: 5px 0;'><strong>Sản phẩm:</strong> {(request.Book?.Title ?? "Phụ kiện/Sách")}</p>
                            <p style='margin: 5px 0;'><strong>Số lượng:</strong> {request.Quantity}</p>
                            <p style='margin: 5px 0;'><strong>Lý do:</strong> {request.Reason}</p>
                            <p style='margin: 5px 0;'><strong>Mô tả chi tiết:</strong> {request.Description ?? "Không có"}</p>
                        </div>
                        <p>Bộ phận CSKH đang tiến hành thẩm định lỗi sản phẩm của bạn qua hình ảnh đính kèm và sẽ liên hệ lại với bạn trong vòng 24h làm việc.</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='font-size: 12px; color: #999; text-align: center;'>&copy; 2026 WedBanSach - Inspired by Fahasa. All rights reserved.</p>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendReturnRequestStatusUpdatedEmailAsync(string toEmail, string userName, ReturnRequest request)
        {
            var subject = $"Cập nhật trạng thái yêu cầu #{request.Id} - WedBanSach";
            var typeText = request.RequestType switch
            {
                "Exchange" => "Đổi hàng",
                "Return" => "Trả hàng",
                "Refund" => "Hoàn tiền",
                "Warranty" => "Bảo hành",
                _ => request.RequestType
            };
            var statusText = request.Status switch
            {
                "Pending" => "Đang chờ xử lý",
                "Approved" => "Đã được phê duyệt",
                "Rejected" => "Bị từ chối",
                "Completed" => "Đã hoàn thành",
                _ => request.Status
            };
            var color = request.Status switch
            {
                "Approved" => "#28a745",
                "Rejected" => "#dc3545",
                "Completed" => "#007bff",
                _ => "#ffc107"
            };

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #fce4ec; border-radius: 10px; background-color: #fff;'>
                        <h2 style='color: #e91e63; text-align: center; border-bottom: 2px solid #fce4ec; padding-bottom: 10px;'>Cập Nhật Trạng Thái Yêu Cầu</h2>
                        <p>Xin chào <strong>{userName}</strong>,</p>
                        <p>Yêu cầu <strong>{typeText}</strong> mã số <strong>#RQ-{request.Id}</strong> của bạn vừa có cập nhật mới:</p>
                        <div style='background-color: #fff5f8; padding: 15px; border-radius: 8px; border: 1px solid #f8cdda; margin: 20px 0;'>
                            <p style='margin: 5px 0; font-size: 18px;'><strong>Trạng thái:</strong> <span style='color: {color}; font-weight: bold;'>{statusText}</span></p>
                            <p style='margin: 5px 0;'><strong>Phản hồi từ Admin:</strong> {request.AdminNote ?? "Đang xử lý"}</p>
                            <p style='margin: 5px 0;'><strong>Thời gian cập nhật:</strong> {request.UpdatedAt:dd/MM/yyyy HH:mm:ss}</p>
                        </div>
                        { (request.Status == "Approved" ? "<p><strong>Bước tiếp theo:</strong> Nhân viên giao hàng sẽ liên hệ với bạn trong 1-2 ngày tới để tiến hành thu hồi sản phẩm lỗi và bàn giao sản phẩm đổi mới/tiến hành bước tiếp theo. Quý khách vui lòng đóng gói sản phẩm cẩn thận.</p>" : "") }
                        { (request.Status == "Rejected" ? "<p>Nếu có bất kỳ thắc mắc nào về lý do từ chối đổi trả, vui lòng phản hồi email này hoặc liên hệ hotline để được hỗ trợ lại.</p>" : "") }
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='font-size: 12px; color: #999; text-align: center;'>&copy; 2026 WedBanSach - Inspired by Fahasa. All rights reserved.</p>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> SendRefundCompletedEmailAsync(string toEmail, string userName, ReturnRequest request, RefundTransaction transaction)
        {
            var subject = $"Hoàn tiền thành công cho yêu cầu #{request.Id} - WedBanSach";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #fce4ec; border-radius: 10px; background-color: #fff;'>
                        <h2 style='color: #28a745; text-align: center; border-bottom: 2px solid #fce4ec; padding-bottom: 10px;'>Hoàn Tiền Thành Công</h2>
                        <p>Xin chào <strong>{userName}</strong>,</p>
                        <p>Chúng tôi vui mừng thông báo giao dịch hoàn tiền cho yêu cầu trả hàng <strong>#RQ-{request.Id}</strong> của quý khách đã hoàn tất thành công.</p>
                        <div style='background-color: #f4fbf7; padding: 15px; border-radius: 8px; border: 1px solid #c3e6cb; margin: 20px 0; color: #155724;'>
                            <p style='margin: 5px 0; font-size: 18px;'><strong>Số tiền hoàn:</strong> <strong>{request.RefundAmount ?? 0:N0} VND</strong></p>
                            <p style='margin: 5px 0;'><strong>Phương thức hoàn:</strong> {transaction.RefundMethod}</p>
                            <p style='margin: 5px 0;'><strong>Mã giao dịch:</strong> {transaction.TransactionCode}</p>
                            <p style='margin: 5px 0;'><strong>Thời gian giao dịch:</strong> {transaction.RefundDate:dd/MM/yyyy HH:mm:ss}</p>
                        </div>
                        <p>Quý khách vui lòng kiểm tra tài khoản ngân hàng hoặc phương thức nhận tiền đã chọn. Số tiền có thể mất 1-3 ngày làm việc để hiển thị số dư tùy thuộc vào ngân hàng thụ hưởng.</p>
                        <p>Cảm ơn quý khách đã tin tưởng mua sắm tại WedBanSach!</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                        <p style='font-size: 12px; color: #999; text-align: center;'>&copy; 2026 WedBanSach - Inspired by Fahasa. All rights reserved.</p>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_senderEmail, _senderName);
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        smtpClient.EnableSsl = true;
                        smtpClient.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                        
                        await smtpClient.SendMailAsync(message);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }
    }
}
