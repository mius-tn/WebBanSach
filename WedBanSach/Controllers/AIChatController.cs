using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Helpers;
using WedBanSach.Models;
using WedBanSach.Services;
using WedBanSach.ViewModels;

namespace WedBanSach.Controllers;

[ApiController]
[Route("api/ai")]
public class AIChatController : ControllerBase
{
    private readonly ChatbotService _chatbotService;
    private readonly BookStoreDbContext _context;
    private const string CART_KEY = "Cart";

    public AIChatController(ChatbotService chatbotService, BookStoreDbContext context)
    {
        _chatbotService = chatbotService;
        _context = context;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { success = false, message = "Nội dung tin nhắn trống" });
        }

        // Get Logged In User from Session
        int? userId = null;
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (int.TryParse(userIdStr, out int parsedId))
        {
            userId = parsedId;
        }

        // Process message through Chatbot Service
        var (session, aiResponse) = await _chatbotService.ProcessMessageAsync(request.SessionId, userId, request.Message);

        // Check for Auto Cart Addition Intent from AI
        bool cartUpdated = false;
        int cartCount = 0;
        string cartMessage = "";

        if (aiResponse.ActionBookId.HasValue && aiResponse.ActionBookId.Value > 0)
        {
            var bookId = aiResponse.ActionBookId.Value;
            var quantity = aiResponse.ActionQuantity ?? 1;

            var book = await _context.Books
                .Include(b => b.BookImages)
                .FirstOrDefaultAsync(b => b.BookID == bookId && b.Status == "Active");

            if (book != null && book.StockQuantity >= quantity)
            {
                var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>(CART_KEY) ?? new CartViewModel();
                var cartItem = cart.Items.FirstOrDefault(c => c.BookID == bookId);

                if (cartItem != null)
                {
                    cartItem.Quantity += quantity;
                }
                else
                {
                    cart.Items.Add(new CartItemViewModel
                    {
                        BookID = book.BookID,
                        Title = book.Title,
                        Price = book.Price,
                        DiscountPrice = book.DiscountPrice,
                        ImageUrl = book.BookImages?.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? "/images/default-book.png",
                        Quantity = quantity
                    });
                }

                HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
                cartUpdated = true;
                cartCount = cart.TotalQuantity;
                cartMessage = $"Đã tự động thêm '{book.Title}' vào giỏ hàng!";
            }
        }

        // Fetch detailed information for recommended book IDs
        List<object> recommendedProducts = new();
        if (aiResponse.RecommendedBookIds != null && aiResponse.RecommendedBookIds.Any())
        {
            var books = await _context.Books
                .Include(b => b.BookImages)
                .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                .Where(b => aiResponse.RecommendedBookIds.Contains(b.BookID) && b.Status == "Active")
                .ToListAsync();

            foreach (var b in books)
            {
                recommendedProducts.Add(new
                {
                    bookId = b.BookID,
                    title = b.Title,
                    price = b.Price,
                    discountPrice = b.DiscountPrice,
                    imageUrl = b.BookImages?.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? "/images/default-book.png",
                    author = string.Join(", ", b.BookAuthors.Select(ba => ba.Author.AuthorName)),
                    stock = b.StockQuantity
                });
            }
        }

        return Ok(new
        {
            success = true,
            sessionId = session.Id,
            text = aiResponse.Text,
            intent = aiResponse.Intent,
            recommendedProducts,
            cartUpdated,
            cartCount,
            cartMessage
        });
    }

    [HttpPost("add-to-cart")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        if (request == null || request.BookId <= 0)
        {
            return BadRequest(new { success = false, message = "Mã sách không hợp lệ" });
        }

        var book = await _context.Books
            .Include(b => b.BookImages)
            .FirstOrDefaultAsync(b => b.BookID == request.BookId && b.Status == "Active");

        if (book == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy cuốn sách này" });
        }

        var quantity = request.Quantity <= 0 ? 1 : request.Quantity;
        if (book.StockQuantity < quantity)
        {
            return BadRequest(new { success = false, message = "Số lượng trong kho không đủ" });
        }

        var cart = HttpContext.Session.GetObjectFromJson<CartViewModel>(CART_KEY) ?? new CartViewModel();
        var cartItem = cart.Items.FirstOrDefault(c => c.BookID == request.BookId);

        if (cartItem != null)
        {
            cartItem.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItemViewModel
            {
                BookID = book.BookID,
                Title = book.Title,
                Price = book.Price,
                DiscountPrice = book.DiscountPrice,
                ImageUrl = book.BookImages?.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? "/images/default-book.png",
                Quantity = quantity
            });
        }

        HttpContext.Session.SetObjectAsJson(CART_KEY, cart);

        return Ok(new
        {
            success = true,
            cartCount = cart.TotalQuantity,
            message = $"Đã thêm '{book.Title}' vào giỏ hàng thành công!"
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(int sessionId)
    {
        if (sessionId <= 0)
        {
            return BadRequest(new { success = false, message = "Mã phiên chat không hợp lệ" });
        }

        var session = await _context.AIChatSessions
            .Include(s => s.AIChatMessages)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phiên chat" });
        }

        var messages = session.AIChatMessages
            .OrderBy(m => m.CreatedAt)
            .Select(m => {
                if (m.SenderType == "AI")
                {
                    try
                    {
                        // Parse JSON structure if it's stored as structured JSON
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var aiRes = JsonSerializer.Deserialize<AIResponse>(m.Message, options);
                        return new
                        {
                            role = "assistant",
                            text = aiRes?.Text ?? m.Message,
                            recommendedBookIds = aiRes?.RecommendedBookIds ?? new List<int>()
                        };
                    }
                    catch
                    {
                        return new
                        {
                            role = "assistant",
                            text = m.Message,
                            recommendedBookIds = new List<int>()
                        };
                    }
                }
                else
                {
                    return new
                    {
                        role = "user",
                        text = m.Message,
                        recommendedBookIds = new List<int>()
                    };
                }
            })
            .ToList();

        // Hydrate product cards for history recommended IDs
        List<object> historyMessages = new();
        foreach (var msg in messages)
        {
            List<object> products = new();
            if (msg.recommendedBookIds != null && msg.recommendedBookIds.Any())
            {
                var books = await _context.Books
                    .Include(b => b.BookImages)
                    .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                    .Where(b => msg.recommendedBookIds.Contains(b.BookID) && b.Status == "Active")
                    .ToListAsync();

                foreach (var b in books)
                {
                    products.Add(new
                    {
                        bookId = b.BookID,
                        title = b.Title,
                        price = b.Price,
                        discountPrice = b.DiscountPrice,
                        imageUrl = b.BookImages?.FirstOrDefault(i => i.IsMain)?.ImageUrl ?? "/images/default-book.png",
                        author = string.Join(", ", b.BookAuthors.Select(ba => ba.Author.AuthorName)),
                        stock = b.StockQuantity
                    });
                }
            }

            historyMessages.Add(new
            {
                role = msg.role,
                text = msg.text,
                recommendedProducts = products
            });
        }

        return Ok(new
        {
            success = true,
            messages = historyMessages
        });
    }
}

public class ChatRequest
{
    public int? SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AddToCartRequest
{
    public int BookId { get; set; }
    public int Quantity { get; set; }
}
