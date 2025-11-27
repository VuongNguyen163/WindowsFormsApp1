using System;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace WindowsFormsApp1.Data
{
    public class GeminiService
    {
        // ⚠️ Thay API Key MỚI của bạn vào đây (Key cũ đã bị lộ, hãy xóa đi)
        private const string API_KEY = "AIzaSyAk_ba0hsSTK45tICz2M3WyRgM1UWcaoq4";

        // SỬA LỖI Ở ĐÂY: Dùng v1beta thay vì v1
        private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
        
        public async Task<string> AskGemini(string contextText, string userPrompt)
        {
            try
            {
                // Cách khởi tạo này an toàn nhất với RestSharp để tránh lỗi mã hóa dấu hai chấm (:)
                var options = new RestClientOptions(API_URL);
                var client = new RestClient(options);

                var request = new RestRequest(""); // Resource rỗng vì URL đã đầy đủ
                request.Method = Method.Post;

                // Thêm key vào query string
                request.AddQueryParameter("key", API_KEY);
                request.AddHeader("Content-Type", "application/json");

                // Cắt bớt văn bản nếu quá dài (tránh lỗi quá tải)
                string safeContext = contextText.Length > 30000 ? contextText.Substring(0, 30000) : contextText;

                string finalPrompt = $"Dựa vào đoạn văn sau:\n---\n{safeContext}\n---\nHãy thực hiện yêu cầu: {userPrompt}";

                var body = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = finalPrompt }
                            }
                        }
                    }
                };

                request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    var jsonResponse = JObject.Parse(response.Content);
                    string aiReply = jsonResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                    return aiReply ?? "AI không trả lời (null).";
                }
                else
                {
                    return $"Lỗi API ({response.StatusCode}): {response.Content}";
                }
            }
            catch (Exception ex)
            {
                return $"Lỗi hệ thống: {ex.Message}";
            }
        }
    }
}