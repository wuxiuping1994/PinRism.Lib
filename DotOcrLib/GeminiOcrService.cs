using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DotOcrLib // Namespace changed to match your project name
{
    /// <summary>
    /// Represents the response structure from the Gemini API for text generation.
    /// </summary>
    public class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    /// <summary>
    /// Represents a candidate response from the Gemini API.
    /// </summary>
    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    /// <summary>
    /// Represents the content part of a Gemini API response.
    /// </summary>
    public class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    /// <summary>
    /// Represents a single part within the content of a Gemini API response.
    /// </summary>
    public class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    /// <summary>
    /// Represents the request payload for the Gemini API.
    /// </summary>
    public class GeminiApiRequest
    {
        [JsonPropertyName("contents")]
        public List<RequestContent>? Contents { get; set; }
    }

    /// <summary>
    /// Represents a content block in the Gemini API request.
    /// </summary>
    public class RequestContent
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user"; // Default role for user input

        [JsonPropertyName("parts")]
        public List<RequestPart> Parts { get; set; } = new List<RequestPart>();
    }

    /// <summary>
    /// Represents a part within the request content, supporting text and inline data (images).
    /// </summary>
    public class RequestPart
    {
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonPropertyName("inlineData")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InlineData? InlineData { get; set; }
    }

    /// <summary>
    /// Represents inline data for images in the Gemini API request.
    /// </summary>
    public class InlineData
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty; // Base64 encoded image data
    }


    /// <summary>
    /// Service for performing Optical Character Recognition (OCR) using Google's Gemini API.
    /// </summary>
    public class GeminiOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiOcrService> _logger;
        private readonly string _geminiApiKey;
        private readonly string _geminiApiUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeminiOcrService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for API calls.</param>
        /// <param name="configuration">The application configuration to retrieve the API key.</param>
        /// <param name="logger">The logger instance.</param>
        public GeminiOcrService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiOcrService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Retrieve Gemini API Key from configuration.
            // It's recommended to store this in appsettings.json or environment variables.
            _geminiApiKey = configuration["GeminiApiKey"] ??
                            throw new InvalidOperationException("GeminiApiKey is not configured.");

            // Construct the Gemini API URL. Using gemini-2.0-flash as requested.
            _geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_geminiApiKey}";

            _logger.LogInformation("GeminiOcrService initialized with API URL: {ApiUrl}", _geminiApiUrl);
        }

        /// <summary>
        /// Extracts text from an image using the Gemini API.
        /// </summary>
        /// <param name="imageData">The image data as a byte array.</param>
        /// <param name="mimeType">The MIME type of the image (e.g., "image/jpeg", "image/png").</param>
        /// <returns>The extracted text, or an empty string if no text is found or an error occurs.</returns>
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
                // Convert image data to base64 string
                string base64ImageData = Convert.ToBase64String(imageData);

                // Prepare the request payload for Gemini API
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

                // Send the POST request to the Gemini API
                var response = await _httpClient.PostAsJsonAsync(_geminiApiUrl, requestPayload);

                // Ensure the response was successful
                response.EnsureSuccessStatusCode();

                // Read and deserialize the response
                var apiResponse = await response.Content.ReadFromJsonAsync<GeminiApiResponse>();

                // Extract the text from the response
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