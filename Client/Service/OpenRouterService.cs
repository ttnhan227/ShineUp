using Client.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Client.Service
{
    public class OpenRouterService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenRouterOptions _options;

        public OpenRouterService(HttpClient httpClient, IOptions<OpenRouterOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> AskAsync(string userInput)
        {
            var requestBody = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a helpful and professional assistant for a Talent Agency website. You assist users in finding talents, posting opportunities, and answering questions clearly in English. Keep responses concise, friendly, and relevant to entertainment, media, and professional networking."
                    },
                    new
                    {
                        role = "user",
                        content = userInput
                    }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Headers.Add("HTTP-Referer", _options.Referer);

            try
            {
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"OpenRouter API error: {response.StatusCode} - {content}");
                }

                dynamic result = JsonConvert.DeserializeObject(content);
                return result?.choices[0]?.message?.content?.ToString() ?? "[No response from AI]";
            }
            catch (Exception ex)
            {
                // Log tại đây nếu cần
                return $"[Lỗi]: {ex.Message}";
            }
        }
    }
}
