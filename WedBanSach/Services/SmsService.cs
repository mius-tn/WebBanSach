using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace WedBanSach.Services
{
    public class SmsService
    {
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private readonly string _brandName;
        private readonly HttpClient _httpClient;

        public SmsService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _apiKey = _configuration["SmsSettings:ESmsApiKey"] ?? "";
            _secretKey = _configuration["SmsSettings:ESmsSecretKey"] ?? "";
            _brandName = _configuration["SmsSettings:BrandName"] ?? "WEDBANSACH";
        }

        public string GenerateOTP()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<bool> SendOTPAsync(string phoneNumber, string otpCode)
        {
            try
            {
                // Format phone number (remove +84, add 0 if needed)
                var formattedPhone = FormatPhoneNumber(phoneNumber);
                
                // ESMS.vn API endpoint
                var apiUrl = "http://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/";
                
                var message = $"Ma xac thuc cua ban la: {otpCode}. Ma nay co hieu luc trong 5 phut.";
                
                var requestData = new
                {
                    ApiKey = _apiKey,
                    SecretKey = _secretKey,
                    Phone = formattedPhone,
                    Content = message,
                    SmsType = 2, // 2 = Brandname OTP
                    Brandname = _brandName
                };

                var jsonContent = JsonSerializer.Serialize(requestData);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(apiUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"ESMS Response: {responseContent}");

                // Parse response
                var responseData = JsonSerializer.Deserialize<ESmsResponse>(responseContent);
                
                return responseData?.CodeResult == "100"; // 100 = Success
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending SMS: {ex.Message}");
                return false;
            }
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            // Remove all non-digit characters
            var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());
            
            // If starts with 84, replace with 0
            if (cleaned.StartsWith("84"))
            {
                cleaned = "0" + cleaned.Substring(2);
            }
            
            // If doesn't start with 0, add it
            if (!cleaned.StartsWith("0"))
            {
                cleaned = "0" + cleaned;
            }
            
            return cleaned;
        }

        private class ESmsResponse
        {
            public string? CodeResult { get; set; }
            public string? CountRegenerate { get; set; }
            public string? SMSID { get; set; }
        }
    }
}
