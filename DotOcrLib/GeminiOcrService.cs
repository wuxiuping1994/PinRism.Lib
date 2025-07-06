using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DotOcrLib
{
    public class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    public class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public class GeminiApiRequest
    {
        [JsonPropertyName("contents")]
        public List<RequestContent>? Contents { get; set; }
    }

    public class RequestContent
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("parts")]
        public List<RequestPart> Parts { get; set; } = new List<RequestPart>();
    }

    public class RequestPart
    {
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonPropertyName("inlineData")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InlineData? InlineData { get; set; }
    }

    public class InlineData
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }

    public class GeminiOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiOcrService> _logger;
        private readonly string _geminiApiKey;
        private readonly string _geminiApiUrl;

        public GeminiOcrService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiOcrService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _geminiApiKey = configuration["GeminiApiKey"] ??
                            throw new InvalidOperationException("GeminiApiKey is not configured.");

            _geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_geminiApiKey}";

            _logger.LogInformation("GeminiOcrService initialized with API URL: {ApiUrl}", _geminiApiUrl);
        }

        public async Task<string> ExtractTextFromImageAsync(byte[] imageData, string mimeType)
        {
            if (imageData == null || imageData.Length == 0)
            {
                _logger.LogWarning("Attempted to extract text from empty image data.");
                return string.Empty;
            }
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                _logger.LogWarning("MIME type not provided for image data.");
                return string.Empty;
            }

            try
            {
                string base64ImageData = Convert.ToBase64String(imageData);

                var requestPayload = new GeminiApiRequest
                {
                    Contents = new List<RequestContent>
                    {
                        new RequestContent
                        {
                            Parts = new List<RequestPart>
                            {
                                new RequestPart { Text = "Extract all text from this image." },
                                new RequestPart { InlineData = new InlineData { MimeType = mimeType, Data = base64ImageData } }
                            }
                        }
                    }
                };

                _logger.LogInformation("Sending request to Gemini API for text extraction. Image MIME Type: {MimeType}", mimeType);

                var response = await _httpClient.PostAsJsonAsync(_geminiApiUrl, requestPayload);

                response.EnsureSuccessStatusCode();

                var apiResponse = await response.Content.ReadFromJsonAsync<GeminiApiResponse>();

                var extractedText = apiResponse?.Candidates?
                                                .FirstOrDefault()?
                                                .Content?
                                                .Parts?
                                                .FirstOrDefault()?
                                                .Text;

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogInformation("No text extracted or response was empty from Gemini API.");
                    return string.Empty;
                }

                _logger.LogInformation("Successfully extracted text from image.");
                return extractedText;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request error calling Gemini API: {StatusCode}", httpEx.StatusCode);
                return string.Empty;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization error from Gemini API response.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during text extraction.");
                return string.Empty;
            }
        }
    }
}
