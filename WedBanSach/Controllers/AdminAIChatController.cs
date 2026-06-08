using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
[Route("admin/ai-chat")]
public class AdminAIChatController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminAIChatController(BookStoreDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        // 1. General Metrics
        var totalSessions = await _context.AIChatSessions.CountAsync();
        var totalMessages = await _context.AIChatMessages.CountAsync();
        var totalRecommendations = await _context.AIRecommendations.CountAsync();

        var convertedRecommendations = 0;
        double conversionRate = 0;

        if (totalRecommendations > 0)
        {
            var conversions = await _context.AIRecommendations
                .Where(r => r.CustomerId != null)
                .Join(_context.Orders,
                      r => r.CustomerId,
                      o => o.UserID,
                      (r, o) => new { Rec = r, Ord = o })
                .Where(x => x.Ord.OrderDate >= x.Rec.CreatedAt)
                .Join(_context.OrderDetails,
                      x => x.Ord.OrderID,
                      od => od.OrderID,
                      (x, od) => new { x.Rec, x.Ord, Detail = od })
                .Where(y => y.Detail.BookID == y.Rec.ProductId)
                .Select(y => new { y.Rec.CustomerId, y.Rec.ProductId })
                .Distinct()
                .CountAsync();

            convertedRecommendations = conversions;
            conversionRate = Math.Round(((double)convertedRecommendations / totalRecommendations) * 100, 2);
        }

        // 2. Data for Chart.js: Daily Sessions (Last 7 Days)
        var sevenDaysAgo = DateTime.Today.AddDays(-6);
        var dailySessionsData = await _context.AIChatSessions
            .Where(s => s.StartedAt >= sevenDaysAgo)
            .GroupBy(s => s.StartedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Fill in missing dates to prevent breaks in the chart
        var dateList = new List<string>();
        var countList = new List<int>();
        for (int i = 0; i < 7; i++)
        {
            var d = DateTime.Today.AddDays(-6 + i);
            dateList.Add(d.ToString("dd/MM"));
            var matched = dailySessionsData.FirstOrDefault(x => x.Date == d.Date);
            countList.Add(matched?.Count ?? 0);
        }

        ViewBag.ChartLabels = JsonSerializer.Serialize(dateList);
        ViewBag.ChartCounts = JsonSerializer.Serialize(countList);

        // 3. Top Books Recommended by AI
        // Group by ProductId in DB first, then load Books with images in memory
        var topProductIds = await _context.AIRecommendations
            .Where(r => r.ProductId != 0)
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var productIds = topProductIds.Select(x => x.ProductId).ToList();
        var booksDict = await _context.Books
            .Include(b => b.BookImages)
            .Where(b => productIds.Contains(b.BookID))
            .ToDictionaryAsync(b => b.BookID);

        var topRecommendedBooks = topProductIds
            .Where(x => booksDict.ContainsKey(x.ProductId))
            .Select(x => new { Book = booksDict[x.ProductId], x.Count })
            .ToList();


        // 4. Recent Chat Sessions for Inspector list
        var recentSessions = await _context.AIChatSessions
            .Join(_context.Users,
                  s => s.CustomerId,
                  u => u.UserID,
                  (s, u) => new { Session = s, User = u })
            .Select(x => new ChatSessionListItem
            {
                SessionId = x.Session.Id,
                CustomerName = x.User.FullName ?? "Khách",
                CustomerEmail = x.User.Email,
                StartedAt = x.Session.StartedAt,
                LastMessageAt = x.Session.LastMessageAt,
                MessageCount = _context.AIChatMessages.Count(m => m.SessionId == x.Session.Id)
            })
            .OrderByDescending(x => x.LastMessageAt)
            .Take(10)
            .ToListAsync();

        // Handle Guest Sessions if any
        var guestSessions = await _context.AIChatSessions
            .Where(s => s.CustomerId == null)
            .Select(x => new ChatSessionListItem
            {
                SessionId = x.Id,
                CustomerName = "Khách vãng lai",
                CustomerEmail = "N/A",
                StartedAt = x.StartedAt,
                LastMessageAt = x.LastMessageAt,
                MessageCount = _context.AIChatMessages.Count(m => m.SessionId == x.Id)
            })
            .OrderByDescending(x => x.LastMessageAt)
            .Take(10)
            .ToListAsync();

        var combinedSessions = recentSessions.Concat(guestSessions)
            .OrderByDescending(x => x.LastMessageAt)
            .Take(15)
            .ToList();

        var stats = new
        {
            TotalSessions = totalSessions,
            TotalMessages = totalMessages,
            TotalRecommendations = totalRecommendations,
            ConvertedRecommendations = convertedRecommendations,
            ConversionRate = conversionRate
        };

        ViewBag.Stats = stats;
        ViewBag.TopRecommendedBooks = topRecommendedBooks;
        ViewBag.RecentSessions = combinedSessions;

        return View("~/Views/Admin/AIDashboard.cshtml");
    }

    [HttpGet("session-logs/{id}")]
    public async Task<IActionResult> GetSessionLogs(int id)
    {
        var session = await _context.AIChatSessions
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phiên chat" });
        }

        var messages = await _context.AIChatMessages
            .Where(m => m.SessionId == id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.SenderType,
                m.Message,
                m.CreatedAt
            })
            .ToListAsync();

        var allBookIds = new HashSet<int>();
        var parsedMessagesData = new List<(int Id, string SenderType, string DisplayedMessage, DateTime CreatedAt, List<int> RecommendedBookIds)>();

        foreach (var m in messages)
        {
            string displayedMessage = m.Message;
            List<int> recBookIds = null;

            if (m.SenderType == "AI")
            {
                try
                {
                    using var doc = JsonDocument.Parse(m.Message);
                    if (doc.RootElement.TryGetProperty("text", out var textProp))
                    {
                        displayedMessage = textProp.GetString() ?? m.Message;
                    }
                    else if (doc.RootElement.TryGetProperty("Text", out var textPropUpper))
                    {
                        displayedMessage = textPropUpper.GetString() ?? m.Message;
                    }

                    if (doc.RootElement.TryGetProperty("recommendedBookIds", out var recProp) && recProp.ValueKind == JsonValueKind.Array)
                    {
                        recBookIds = new List<int>();
                        foreach (var el in recProp.EnumerateArray())
                        {
                            if (el.TryGetInt32(out int bid))
                            {
                                recBookIds.Add(bid);
                                allBookIds.Add(bid);
                            }
                        }
                    }
                    else if (doc.RootElement.TryGetProperty("RecommendedBookIds", out var recPropUpper) && recPropUpper.ValueKind == JsonValueKind.Array)
                    {
                        recBookIds = new List<int>();
                        foreach (var el in recPropUpper.EnumerateArray())
                        {
                            if (el.TryGetInt32(out int bid))
                            {
                                recBookIds.Add(bid);
                                allBookIds.Add(bid);
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback to raw string
                }
            }

            parsedMessagesData.Add((m.Id, m.SenderType, displayedMessage, m.CreatedAt, recBookIds));
        }

        var booksDict = new Dictionary<int, Book>();
        if (allBookIds.Any())
        {
            booksDict = await _context.Books
                .Include(b => b.BookImages)
                .Where(b => allBookIds.Contains(b.BookID))
                .ToDictionaryAsync(b => b.BookID);
        }

        var processedMessages = parsedMessagesData.Select(m =>
        {
            object recommendedBooks = null;
            if (m.RecommendedBookIds != null && m.RecommendedBookIds.Any())
            {
                var books = m.RecommendedBookIds
                    .Where(bid => booksDict.ContainsKey(bid))
                    .Select(bid => 
                    {
                        var b = booksDict[bid];
                        var mainImg = b.BookImages?.FirstOrDefault()?.ImageUrl ?? "/images/default-book.png";
                        return new {
                            b.BookID,
                            b.Title,
                            b.Price,
                            b.DiscountPrice,
                            ImageUrl = mainImg
                        };
                    }).ToList();
                    
                if (books.Any()) {
                    recommendedBooks = books;
                }
            }

            return new
            {
                m.Id,
                m.SenderType,
                message = m.DisplayedMessage,
                createdAt = m.CreatedAt.ToString("HH:mm - dd/MM/yyyy"),
                recommendedBooks
            };
        }).ToList();

        return Json(new { success = true, messages = processedMessages });
    }
}

public class ChatSessionListItem
{
    public int SessionId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}
