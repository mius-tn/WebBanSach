using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Services;

public class ChatbotService
{
    private readonly BookStoreDbContext _context;
    private readonly AIService _aiService;
    private readonly RecommendationService _recommendationService;
    private readonly IConfiguration _configuration;

    public ChatbotService(BookStoreDbContext context, AIService aiService, RecommendationService recommendationService, IConfiguration configuration)
    {
        _context = context;
        _aiService = aiService;
        _recommendationService = recommendationService;
        _configuration = configuration;
    }

    /// <summary>
    /// Processes a chat message from a customer, gathers context, calls AI, and returns the response.
    /// </summary>
    public async Task<(AIChatSession Session, AIResponse Response)> ProcessMessageAsync(int? sessionId, int? userId, string userMessage)
    {
        // 1. Get or Create Session
        AIChatSession session;
        if (sessionId.HasValue && sessionId.Value > 0)
        {
            session = await _context.AIChatSessions
                .Include(s => s.AIChatMessages)
                .FirstOrDefaultAsync(s => s.Id == sessionId.Value) ?? new AIChatSession();
        }
        else
        {
            session = new AIChatSession
            {
                CustomerId = userId,
                StartedAt = DateTime.Now,
                LastMessageAt = DateTime.Now
            };
            _context.AIChatSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        // Save User Message
        var userMsg = new AIChatMessage
        {
            SessionId = session.Id,
            SenderType = "User",
            Message = userMessage,
            CreatedAt = DateTime.Now
        };
        _context.AIChatMessages.Add(userMsg);
        session.LastMessageAt = DateTime.Now;
        await _context.SaveChangesAsync();

        // 2. Fetch Chat History (Last 10 messages for conversation memory)
        var historyMessages = await _context.AIChatMessages
            .Where(m => m.SessionId == session.Id)
            .OrderBy(m => m.CreatedAt)
            .Take(12)
            .Select(m => new ChatMessageDto
            {
                Role = m.SenderType == "User" ? "user" : "assistant",
                Content = m.Message
            })
            .ToListAsync();

        // 3. Assemble Dynamic Context (RAG)
        string bookCatalogContext = "";
        string orderHistoryContext = "";
        string policyContext = "";

        // RAG Part A: Search books matching query
        var matchedBooks = await _recommendationService.GetMatchingBooksAsync(userMessage, 8);
        if (matchedBooks.Any())
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Danh sách sách phù hợp trong kho:");
            foreach (var b in matchedBooks)
            {
                sb.AppendLine($"- ID: {b.BookID} | Tên: {b.Title} | Giá gốc: {b.Price:N0}đ | Giá bán: {(b.DiscountPrice ?? b.Price):N0}đ | Còn lại: {b.StockQuantity} cuốn | Đã bán: {b.SoldQuantity}");
            }
            bookCatalogContext = sb.ToString();
        }

        // RAG Part B: Order history if logged in
        if (userId.HasValue)
        {
            var orders = await _context.Orders
                .Where(o => o.UserID == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .Take(3)
                .ToListAsync();

            if (orders.Any())
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Lịch sử đơn hàng gần đây của khách hàng:");
                foreach (var o in orders)
                {
                    sb.AppendLine($"- Mã ĐH: #{o.OrderID} | Ngày đặt: {o.OrderDate:dd/MM/yyyy} | Tổng tiền: {o.TotalAmount:N0}đ | Trạng thái: {o.OrderStatus} | Đia chỉ: {o.ShippingAddress}");
                }
                orderHistoryContext = sb.ToString();
            }
        }

        // RAG Part C: Retrieve return/warranty policies
        var policies = await _context.Policies
            .Include(p => p.Category)
            .Where(p => p.IsPublished)
            .ToListAsync();
        if (policies.Any())
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Chính sách Đổi trả, Hoàn tiền và Bảo hành của cửa hàng:");
            foreach (var p in policies)
            {
                // Strip HTML tags for cleaner prompt
                var plainText = System.Text.RegularExpressions.Regex.Replace(p.Content, "<.*?>", String.Empty);
                sb.AppendLine($"- [{p.Category?.Name ?? p.Title}]: {plainText.Substring(0, Math.Min(350, plainText.Length))}...");
            }
            policyContext = sb.ToString();
        }

        // 4. Construct AI System Prompt
        var systemPrompt = $@"
Bạn là 'MiuMiu' - Linh vật mèo hồng và cũng là Trợ lý AI tư vấn bán sách thông minh, tận tâm của website 'WebBanSach'.
Mục tiêu của bạn là giúp khách hàng tìm sách, đặt mua sách, tra cứu đơn hàng, tư vấn chính sách dịch vụ và chăm sóc khách hàng tự nhiên như một chuyên viên bán hàng thực tế.

[HƯỚNG DẪN GIAO TIẾP]
- Hãy trả lời bằng tiếng Việt lịch sự, trẻ trung, thân thiện và ấm áp. Thường xuyên sử dụng biểu tượng cảm xúc dễ thương (mèo 🐾, sách 📚, tim 💖...).
- Thể hiện sự hiểu biết, nhiệt tình và am hiểu về sách. Tư vấn chi tiết, giới thiệu lý do cụ thể vì sao sách phù hợp với nhu cầu.

[DỮ LIỆU THỰC TẾ TỪ HỆ THỐNG]
Dưới đây là dữ liệu chính xác từ hệ thống bán sách. Hãy dựa vào đây để trả lời khách hàng, TUYỆT ĐỐI không bịa đặt thông tin sách hay đơn hàng không có thực:

{bookCatalogContext}

{orderHistoryContext}

{policyContext}

[HƯỚNG DẪN XỬ LÝ Ý ĐỊNH]
1. Nếu khách muốn tìm sách / gợi ý sách: Hãy tư vấn nhiệt tình dựa trên danh sách sách ở trên. Điền các ID của sách bạn đề xuất vào mảng `recommendedBookIds`.
2. Nếu khách muốn đặt mua / thêm vào giỏ: Hãy điền ID cuốn sách vào `actionBookId` và số lượng vào `actionQuantity` để hệ thống tự động thêm sách cho khách! Báo cho khách biết bạn đã thêm vào giỏ.
3. Nếu khách hỏi đơn hàng: Dựa vào 'Lịch sử đơn hàng gần đây' để trả lời cụ thể tình trạng (Chờ duyệt, Đang giao, Đã giao...).
4. Nếu khách hỏi chính sách: Dựa vào mục 'Chính sách Đổi trả, Hoàn tiền...' để tư vấn chi tiết, rõ ràng thời gian (đổi trả trong 7 ngày, sách phải còn nguyên màng co, hoàn tiền trong 3-5 ngày...).

[YÊU CẦU ĐỊNH DẠNG ĐẦU RA]
Bắt buộc bạn phải trả về một đối tượng JSON thuần túy có cấu trúc chính xác như sau, không chứa các khối markdown hay chữ dư thừa bên ngoài:
{{
  ""text"": ""Câu trả lời hội thoại bằng tiếng Việt gửi cho khách hàng"",
  ""recommendedBookIds"": [12, 45],
  ""intent"": ""general | search_books | add_to_cart | order_status | policy"",
  ""actionBookId"": null,
  ""actionQuantity"": null
}}
";

        // 5. Call OpenAI via AIService, fallback to smart local response on error or placeholder key
        AIResponse aiResponse;
        var actualKey = _configuration["OpenAI:ApiKey"] ?? "";
        
        if (string.IsNullOrEmpty(actualKey) || actualKey == "YOUR_OPENAI_API_KEY" || actualKey.StartsWith("YOUR_"))
        {
            aiResponse = await GetLocalSmartResponse(userMessage, userId, matchedBooks);
        }
        else
        {
            aiResponse = await _aiService.ChatAsync(systemPrompt, historyMessages);
            // If OpenAI API fails (returns default/error response)
            if (aiResponse.Text.Contains("lỗi xảy ra khi kết nối") || aiResponse.Text.Contains("chưa được cấu hình API Key"))
            {
                aiResponse = await GetLocalSmartResponse(userMessage, userId, matchedBooks);
            }
        }

        // 6. Save AI Response in Database
        var aiMsg = new AIChatMessage
        {
            SessionId = session.Id,
            SenderType = "AI",
            Message = JsonSerializer.Serialize(aiResponse), // Save full response or just text
            CreatedAt = DateTime.Now
        };
        _context.AIChatMessages.Add(aiMsg);
        session.LastMessageAt = DateTime.Now;
        await _context.SaveChangesAsync();

        // Save preferences asynchronously if AI detects user interests
        if (userId.HasValue && aiResponse.Intent == "search_books" && matchedBooks.Any())
        {
            var genres = string.Join(",", matchedBooks.SelectMany(b => b.BookCategories.Select(bc => bc.Category.CategoryName)).Distinct().Take(3));
            var authors = string.Join(",", matchedBooks.SelectMany(b => b.BookAuthors.Select(ba => ba.Author.AuthorName)).Distinct().Take(2));
            await _recommendationService.SavePreferencesAsync(userId.Value, genres, authors, null);
        }

        return (session, aiResponse);
    }

    private async Task<AIResponse> GetLocalSmartResponse(string userMessage, int? userId, List<Book> matchedBooks)
    {
        var msg = userMessage.ToLower().Trim();
        var response = new AIResponse();

        // 1. Policy Intent
        if (msg.Contains("đổi") || msg.Contains("trả") || msg.Contains("hoàn tiền") || msg.Contains("bảo hành") || msg.Contains("chính sách"))
        {
            response.Intent = "policy";
            var policies = await _context.Policies.Include(p => p.Category).Where(p => p.IsPublished).ToListAsync();
            var policy = policies.FirstOrDefault(p => msg.Contains(p.Category?.Slug ?? "") || msg.Contains(p.Title.ToLower()));
            if (policy == null) policy = policies.FirstOrDefault();

            if (policy != null)
            {
                var plainText = System.Text.RegularExpressions.Regex.Replace(policy.Content, "<.*?>", string.Empty);
                response.Text = $"🌸 MiuMiu xin thông tin đến bạn về **{policy.Category?.Name ?? policy.Title}** nha:\n\n{plainText.Substring(0, Math.Min(400, plainText.Length))}...\n\nBạn có cần tớ hướng dẫn gửi yêu cầu đổi trả trực tiếp trên web không ạ? 😊";
            }
            else
            {
                response.Text = "🌸 Chính sách của WebBanSach hỗ trợ đổi trả sản phẩm lỗi sản xuất trong vòng 7 ngày kể từ khi nhận hàng thành công, và hoàn tiền từ 3-5 ngày làm việc. Bạn cần tớ hỗ trợ cụ thể trường hợp nào không?";
            }
        }
        // 2. Order Status Intent
        else if (msg.Contains("đơn hàng") || msg.Contains("tra cứu") || msg.Contains("vận chuyển") || msg.Contains("giao hàng"))
        {
            response.Intent = "order_status";
            if (userId.HasValue)
            {
                var latestOrder = await _context.Orders
                    .Where(o => o.UserID == userId.Value)
                    .OrderByDescending(o => o.OrderDate)
                    .FirstOrDefaultAsync();

                if (latestOrder != null)
                {
                    response.Text = $"📦 Dạ MiuMiu đã kiểm tra hệ thống! Đơn hàng gần nhất của bạn là **#{latestOrder.OrderID}** đặt ngày {latestOrder.OrderDate:dd/MM/yyyy}.\n\n- **Tổng tiền:** {latestOrder.TotalAmount:N0}đ\n- **Trạng thái đơn:** {latestOrder.OrderStatus}\n- **Địa chỉ giao:** {latestOrder.ShippingAddress}\n\nMiuMiu sẽ cập nhật trạng thái mới nhất cho bạn ngay khi có thay đổi nhé! 🚚💨";
                }
                else
                {
                    response.Text = "📦 Dạ MiuMiu kiểm tra thấy tài khoản của bạn chưa có đơn hàng nào được đặt gần đây. Bạn có muốn tớ gợi ý một số cuốn sách bán chạy nhất để đặt hàng không ạ? 📚💖";
                }
            }
            else
            {
                response.Text = "📦 Dạ để tra cứu trạng thái đơn hàng trực tiếp, bạn vui lòng **Đăng nhập** tài khoản mua hàng trước nhé! Hoặc bạn có thể cung cấp Mã đơn hàng để tớ kiểm tra giúp nha. 😊";
            }
        }
        // 3. Add to Cart Intent
        else if ((msg.Contains("mua") || msg.Contains("thêm vào giỏ") || msg.Contains("lấy cuốn") || msg.Contains("cho giỏ")) && matchedBooks.Any())
        {
            response.Intent = "add_to_cart";
            var targetBook = matchedBooks.First();
            response.ActionBookId = targetBook.BookID;
            response.ActionQuantity = 1;
            response.RecommendedBookIds = new List<int> { targetBook.BookID };
            response.Text = $"🐾 MiuMiu đã tự động thêm cuốn sách **'{targetBook.Title}'** vào giỏ hàng của bạn rồi đấy! \n\n🛒 Giỏ hàng đã được cập nhật số lượng thành công. Bạn có muốn tiếp tục chọn thêm cuốn nào nữa không để tớ bỏ vào giỏ luôn nha? 😊💖";
        }
        // 4. Search Books Intent
        else if (msg.Contains("sách") || msg.Contains("tìm") || msg.Contains("muốn") || msg.Contains("đề xuất") || msg.Contains("gợi ý") || msg.Contains("giới thiệu") || msg.Contains("hay") || matchedBooks.Any())
        {
            response.Intent = "search_books";
            
            // If matchedBooks is empty, let's fetch top bestsellers as dynamic suggestions!
            var targetBooks = matchedBooks.Any() ? matchedBooks : await _recommendationService.GetAIRecommendationsAsync(userId, 3);
            
            if (targetBooks.Any())
            {
                response.RecommendedBookIds = targetBooks.Select(b => b.BookID).ToList();
                var bookListText = string.Join("\n", targetBooks.Take(3).Select(b => $"- **{b.Title}** (Giá: {(b.DiscountPrice ?? b.Price):N0}đ)"));
                
                if (matchedBooks.Any())
                {
                    response.Text = $"📚 MiuMiu đã tìm thấy các tác phẩm cực kỳ xuất sắc đúng nhu cầu của bạn đây:\n\n{bookListText}\n\nTớ đã hiển thị danh sách chi tiết kèm nút *Mua nhanh* ở ngay bên dưới. Bạn xem có ưng ý tác phẩm nào không nha! 🥰🐾";
                }
                else
                {
                    response.Text = $"📚 Dạ các sách MiuMiu giới thiệu đều là những cuốn sách chất lượng cực kỳ hay và đang được yêu thích nhất đấy ạ! 🥰 MiuMiu gửi bạn một số tác phẩm nổi bật đang bán chạy nhất tại WebBanSach để bạn tham khảo nhé:\n\n{bookListText}\n\nTớ đã hiển thị đầy đủ thẻ sách ở ngay bên dưới, bạn xem qua nha! 💖🐾";
                }
            }
            else
            {
                response.Text = "📚 WebBanSach luôn cam kết cung cấp những cuốn sách chất lượng nhất! Hiện tại do số lượng kho đang cập nhật, bạn có thể nhập tên cuốn sách cụ thể (ví dụ: 'C#' hoặc 'Đám Trẻ') để MiuMiu tìm kiếm chính xác giúp bạn nhé! 💖";
            }
        }
        // 5. General Chat
        else
        {
            response.Intent = "general";
            response.Text = "Chào bạn! Tớ là MiuMiu 🐾, trợ lý tư vấn Bookstore. Tớ có thể giúp bạn:\n- 🔍 Tìm kiếm sách theo yêu cầu\n- 🛒 Thêm nhanh sách vào giỏ hàng\n- 📦 Tra cứu tình trạng đơn hàng của bạn\n- 🌸 Hỏi đáp chính sách đổi trả/hoàn tiền/bảo hành.\n\nBạn cứ nhắn cho tớ biết nhu cầu của bạn nha! 😊💖";
        }

        return response;
    }
}
