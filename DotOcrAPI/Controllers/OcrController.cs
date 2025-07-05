using Microsoft.AspNetCore.Mvc;
using DotOcrLib; // Namespace changed to match your project name
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotOcrAPI.Controllers // Namespace changed to match your project name
{
    [ApiController]
    [Route("api/[controller]")]
    public class OcrController : ControllerBase
    {
        private readonly GeminiOcrService _ocrService;
        private readonly ILogger<OcrController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcrController"/> class.
        /// </summary>
        /// <param name="ocrService">The Gemini OCR service instance.</param>
        /// <param name="logger">The logger instance.</param>
        public OcrController(GeminiOcrService ocrService, ILogger<OcrController> logger)
        {
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Extracts text from an uploaded image.
        /// </summary>
        /// <param name="file">The image file to process.</param>
        /// <returns>A string containing the extracted text.</returns>
        [HttpPost("extract-text")]
        [Consumes("multipart/form-data")]
        [Produces("text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExtractText([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file uploaded or file is empty.");
                return BadRequest("No file uploaded or file is empty.");
            }

            // Basic MIME type validation
            // Gemini API supports common image formats like image/jpeg, image/png, image/webp, image/heic, image/heif.
            // You might want a more robust validation here.
            if (!file.ContentType.StartsWith("image/"))
            {
                _logger.LogWarning("Unsupported file type uploaded: {ContentType}", file.ContentType);
                return BadRequest("Unsupported file type. Please upload an image (e.g., JPEG, PNG).");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                byte[] imageData = memoryStream.ToArray();

                _logger.LogInformation("Received image file: {FileName}, Size: {Length} bytes, Type: {ContentType}",
                                       file.FileName, file.Length, file.ContentType);

                string extractedText = await _ocrService.ExtractTextFromImageAsync(imageData, file.ContentType);

                if (string.IsNullOrEmpty(extractedText))
                {
                    _logger.LogInformation("Text extraction completed, but no text was found for file: {FileName}", file.FileName);
                    return Ok("No text found in the image.");
                }

                _logger.LogInformation("Text successfully extracted from file: {FileName}", file.FileName);
                return Ok(extractedText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the image for text extraction.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during text extraction.");
            }
        }
    }
}
