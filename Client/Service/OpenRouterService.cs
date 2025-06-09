using Client.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace Client.Service
{
    /// <summary>
    /// Service for sending multilingual queries to OpenRouter API and receiving AI responses in the user's language.
    /// </summary>
    public class OpenRouterService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenRouterOptions _options;

        public OpenRouterService(HttpClient httpClient, IOptions<OpenRouterOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        /// <summary>
        /// Sends a chat request to OpenRouter, using language detection to guide AI responses in the correct language.
        /// </summary>
        /// <param name="userInput">The user’s message</param>
        /// <returns>The AI response as a string</returns>
        public async Task<string> AskAsync(string userInput)
        {
            // Step 1: Detect the user's input language.
            var userLanguage = DetectLanguage(userInput);

            // Step 2: Build a multilingual-aware system prompt to instruct the assistant on expected behavior.
            var requestBody = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = $@"
You are a professional and knowledgeable assistant chatbot for ShineUp, a Talent Showcasing Platform.

Your role is to:
- Help users understand how to upload and share videos of their talents.
- Guide users to engage with the community.
- Explain talent discovery and community features.
- Encourage positive and supportive communication.

Important instructions:
- The user's message language is: '{userLanguage}'.
- Always respond in that language unless explicitly asked to switch.
- Do NOT reply in another language even partially.
- Maintain a professional yet warm tone.
"
                    },
                    new
                    {
                        role = "user",
                        content = userInput
                    }
                }
            };

            // Prepare HTTP request
            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            };

            // Set authorization and custom headers
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Headers.Add("HTTP-Referer", _options.Referer);

            try
            {
                // Send request to OpenRouter
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"OpenRouter API error: {response.StatusCode} - {content}");
                }

                // Deserialize response and extract the assistant’s message
                dynamic result = JsonConvert.DeserializeObject(content);
                return result?.choices[0]?.message?.content?.ToString() ?? "[No response from AI]";
            }
            catch (Exception ex)
            {
                // Handle API or network error
                return $"[Error]: {ex.Message}";
            }
        }

        /// <summary>
        /// Basic rule-based language detection using Unicode and keyword heuristics.
        /// </summary>
        /// <param name="input">User input text</param>
        /// <returns>Language code (ISO 639-1)</returns>
private string DetectLanguage(string input)
{
    // Unicode script-based detection (converted to .NET-compatible ranges)

    if (Regex.IsMatch(input, @"[\u0400-\u04FF]")) return "ru"; // Cyrillic
    if (Regex.IsMatch(input, @"[\u0600-\u06FF]")) return "ar"; // Arabic
    if (Regex.IsMatch(input, @"[\u0590-\u05FF]")) return "he"; // Hebrew
    if (Regex.IsMatch(input, @"[\uAC00-\uD7AF]")) return "ko"; // Hangul
    if (Regex.IsMatch(input, @"[\u3040-\u309F\u30A0-\u30FF]")) return "ja"; // Hiragana + Katakana
    if (Regex.IsMatch(input, @"[\u4E00-\u9FFF]")) return "zh"; // Han (CJK)
    if (Regex.IsMatch(input, @"[\u0900-\u097F]")) return "hi"; // Devanagari
    if (Regex.IsMatch(input, @"[\u0E00-\u0E7F]")) return "th"; // Thai
    if (Regex.IsMatch(input, @"[\u0980-\u09FF]")) return "bn"; // Bengali
    if (Regex.IsMatch(input, @"[\u0B80-\u0BFF]")) return "ta"; // Tamil
    if (Regex.IsMatch(input, @"[\u0A80-\u0AFF]")) return "gu"; // Gujarati
    if (Regex.IsMatch(input, @"[\u0C80-\u0CFF]")) return "kn"; // Kannada
    if (Regex.IsMatch(input, @"[\u0A00-\u0A7F]")) return "pa"; // Gurmukhi
    if (Regex.IsMatch(input, @"[\u1200-\u137F]")) return "am"; // Ethiopic
    if (Regex.IsMatch(input, @"[\u0370-\u03FF]")) return "el"; // Greek

    // Latin-based languages with diacritics
    if (Regex.IsMatch(input, @"[à-ỹÀ-Ỹ]")) return "vi"; // Vietnamese
    if (Regex.IsMatch(input, @"[éèêëàâäîïôöùûüç]")) return "fr"; // French
    if (Regex.IsMatch(input, @"[ñáéíóúü¡¿]")) return "es"; // Spanish
    if (Regex.IsMatch(input, @"[äöüß]")) return "de"; // German
    if (Regex.IsMatch(input, @"[åäö]")) return "sv"; // Swedish
    if (Regex.IsMatch(input, @"[æøå]")) return "no"; // Norwegian
    if (Regex.IsMatch(input, @"[ðþæö]")) return "is"; // Icelandic
    if (Regex.IsMatch(input, @"[çãõ]")) return "pt"; // Portuguese
    if (Regex.IsMatch(input, @"[čćžšđ]")) return "hr"; // Croatian/Serbian
    if (Regex.IsMatch(input, @"[șțăâî]")) return "ro"; // Romanian
    if (Regex.IsMatch(input, @"[łżźćń]")) return "pl"; // Polish
    if (Regex.IsMatch(input, @"[čšěřůž]")) return "cs"; // Czech
    if (Regex.IsMatch(input, @"[őű]")) return "hu"; // Hungarian
    if (Regex.IsMatch(input, @"[ğüşöçİ]")) return "tr"; // Turkish
    if (Regex.IsMatch(input, @"[õäöü]")) return "et"; // Estonian
    if (Regex.IsMatch(input, @"[ųėįū]")) return "lt"; // Lithuanian
    if (Regex.IsMatch(input, @"[āēīū]")) return "lv"; // Latvian
    if (Regex.IsMatch(input, @"[əğçşöü]")) return "az"; // Azerbaijani

    // Latin keyword-based heuristics
    if (Regex.IsMatch(input, @"\b(saya|anda|kamu|tidak|apa)\b", RegexOptions.IgnoreCase)) return "id"; // Indonesian
    if (Regex.IsMatch(input, @"\b(ako|ikaw|sila|kayo|natin)\b", RegexOptions.IgnoreCase)) return "fil"; // Filipino
    if (Regex.IsMatch(input, @"\b(malay|saya|tidak|apa)\b", RegexOptions.IgnoreCase)) return "ms"; // Malay
    if (Regex.IsMatch(input, @"\b(eu|você|não|sim|como)\b", RegexOptions.IgnoreCase)) return "pt"; // Portuguese (fallback)

    // Latin default fallback
    if (Regex.IsMatch(input, @"[a-zA-Z]")) return "en";

    // Final fallback
    return "en";
}
    }
}
