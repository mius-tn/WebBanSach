using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WedBanSach.Services;

public class AIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiUrl;
    private readonly string _model;
    private readonly ILogger<AIService> _logger;

    public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
        _apiUrl = configuration["OpenAI:ApiUrl"] ?? "https://api.openai.com/v1/chat/completions";
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
    }

    public async Task<AIResponse> ChatAsync(string systemPrompt, List<ChatMessageDto> messages)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return new AIResponse
            {
                Text = "Xin chào! Hiện tại hệ thống AI Chatbot chưa được cấu hình API Key. Quý khách vui lòng liên hệ quản trị viên hoặc thiết lập trong file appsettings.json nhé!",
                RecommendedBookIds = new List<int>(),
                Intent = "general"
            };
        }

        try
        {
            var requestMessages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            foreach (var msg in messages)
            {
                requestMessages.Add(new { role = msg.Role, content = msg.Content });
            }

            var requestBody = new
            {
                model = _model,
                messages = requestMessages,
                response_format = new { type = "json_object" },
                temperature = 0.5
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogError($"OpenAI API Error: {response.StatusCode} - {errorText}");
                return new AIResponse
                {
                    Text = "Rất tiếc, đã có lỗi xảy ra khi kết nối với máy chủ AI. Bạn hãy thử lại sau ít phút nhé!",
                    RecommendedBookIds = new List<int>(),
                    Intent = "general"
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var assistantContent = choice.GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrEmpty(assistantContent))
            {
                return new AIResponse
                {
                    Text = "Xin lỗi, tôi không thể xử lý yêu cầu lúc này.",
                    RecommendedBookIds = new List<int>(),
                    Intent = "general"
                };
            }

            // Parse structured JSON response from AI model
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var aiResponse = JsonSerializer.Deserialize<AIResponse>(assistantContent, options);

            return aiResponse ?? new AIResponse { Text = assistantContent, Intent = "general" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calling AI Service");
            return new AIResponse
            {
                Text = "Đã có sự cố kết nối dịch vụ tư vấn AI. Tôi sẽ cố gắng phản hồi lại ngay lập tức!",
                RecommendedBookIds = new List<int>(),
                Intent = "general"
            };
        }
    }
}

public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
}

public class AIResponse
{
    public string Text { get; set; } = string.Empty;
    public List<int> RecommendedBookIds { get; set; } = new List<int>();
    public string Intent { get; set; } = "general"; // general, search_books, add_to_cart, order_status, policy
    public int? ActionBookId { get; set; }
    public int? ActionQuantity { get; set; }
}
