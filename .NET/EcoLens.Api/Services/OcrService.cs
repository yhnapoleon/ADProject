using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using SkiaSharp;

namespace EcoLens.Api.Services;

/// <summary>
/// OCR 服务实现（使用 Google Cloud Vision API）
/// </summary>
public class OcrService : IOcrService
{
	private readonly HttpClient _httpClient;
	private readonly string _apiKey;
	private readonly ILogger<OcrService> _logger;

	public OcrService(
		HttpClient httpClient,
		IConfiguration configuration,
		ILogger<OcrService> logger)
	{
		_httpClient = httpClient;
		_apiKey = configuration["GoogleCloud:VisionApiKey"] ?? throw new InvalidOperationException("GoogleCloud:VisionApiKey is not configured");
		_logger = logger;
	}

	/// <summary>
	/// 识别图片或PDF中的文字（从Stream）
	/// </summary>
	public async Task<OcrResult?> RecognizeTextAsync(Stream imageStream, CancellationToken ct = default)
	{
		try
		{
			// 将 Stream 转换为字节数组
			using var memoryStream = new MemoryStream();
			await imageStream.CopyToAsync(memoryStream, ct);
			var imageBytes = memoryStream.ToArray();

			return await RecognizeTextAsync(imageBytes, ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error reading image stream for OCR");
			return null;
		}
	}

	/// <summary>
	/// 识别图片或PDF中的文字（从字节数组）
	/// </summary>
	public async Task<OcrResult?> RecognizeTextAsync(byte[] imageBytes, CancellationToken ct = default)
	{
		try
		{
			// 检查文件大小（Google Vision API 限制为 20MB）
			if (imageBytes.Length > 20 * 1024 * 1024)
			{
				_logger.LogWarning("Image size exceeds 20MB limit: {Size} bytes", imageBytes.Length);
				return null;
			}

			// 检查是否为PDF文件（通过文件头判断）
			var isPdf = imageBytes.Length >= 4 && 
			            imageBytes[0] == 0x25 && imageBytes[1] == 0x50 && 
			            imageBytes[2] == 0x44 && imageBytes[3] == 0x46; // %PDF

			if (isPdf)
			{
				_logger.LogInformation("Detected PDF file, converting to images for OCR");
				return await ProcessPdfAsync(imageBytes, ct);
			}

			// 将图片转换为 base64
			var base64Image = Convert.ToBase64String(imageBytes);

			// 构建请求 JSON
			var requestBody = new
			{
				requests = new[]
				{
					new
					{
						image = new
						{
							content = base64Image
						},
						features = new[]
						{
							new
							{
								type = "DOCUMENT_TEXT_DETECTION",
								maxResults = 1
							}
						}
					}
				}
			};

			// 发送请求到 Google Cloud Vision API
			var url = $"https://vision.googleapis.com/v1/images:annotate?key={_apiKey}";
			var jsonContent = JsonSerializer.Serialize(requestBody);
			var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync(url, content, ct);

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync(ct);
				_logger.LogError("Google Vision API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
				return null;
			}

			var responseJson = await response.Content.ReadAsStringAsync(ct);
			var visionResponse = JsonSerializer.Deserialize<VisionApiResponse>(responseJson, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (visionResponse?.Responses == null || visionResponse.Responses.Count == 0)
			{
				_logger.LogWarning("No responses from Google Vision API");
				return null;
			}

			var firstResponse = visionResponse.Responses[0];

			// 检查是否有错误
			if (firstResponse.Error != null)
			{
				_logger.LogError("Google Vision API error: {Code}, {Message}", firstResponse.Error.Code, firstResponse.Error.Message);
				return null;
			}

			// 提取文本和置信度
			var fullTextAnnotation = firstResponse.FullTextAnnotation;
			if (fullTextAnnotation == null)
			{
				_logger.LogWarning("No text detected in image");
				return new OcrResult
				{
					Text = string.Empty,
					Confidence = 0,
					Pages = 1
				};
			}

			var text = fullTextAnnotation.Text ?? string.Empty;
			var confidence = fullTextAnnotation.Pages?.Count > 0
				? fullTextAnnotation.Pages[0].Confidence ?? 0
				: 0;

			_logger.LogInformation("OCR completed: {TextLength} characters, Confidence: {Confidence}", text.Length, confidence);

			return new OcrResult
			{
				Text = text,
				Confidence = (decimal)confidence,
				Pages = fullTextAnnotation.Pages?.Count ?? 1
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during OCR recognition");
			return null;
		}
	}

	// Google Vision API 响应模型
	private class VisionApiResponse
	{
		public List<VisionResponse>? Responses { get; set; }
	}

	private class VisionResponse
	{
		public VisionError? Error { get; set; }
		public FullTextAnnotation? FullTextAnnotation { get; set; }
	}

	private class VisionError
	{
		public int? Code { get; set; }
		public string? Message { get; set; }
	}

	private class FullTextAnnotation
	{
		public string? Text { get; set; }
		public List<Page>? Pages { get; set; }
	}

	private class Page
	{
		public double? Confidence { get; set; }
	}

	/// <summary>
	/// 处理PDF文件：提取文本内容
	/// </summary>
	private Task<OcrResult?> ProcessPdfAsync(byte[] pdfBytes, CancellationToken ct = default)
	{
		try
		{
			using var pdfStream = new MemoryStream(pdfBytes);
			using var document = PdfDocument.Open(pdfStream);

			var allText = new StringBuilder();
			var totalConfidence = 0.0;
			var pageCount = 0;

			// 处理每一页
			foreach (var page in document.GetPages())
			{
				pageCount++;
				_logger.LogDebug("Processing PDF page {PageNumber} of {TotalPages}", pageCount, document.NumberOfPages);

				// 提取PDF页面的文本（如果PDF包含文本层）
				var pageText = page.Text;
				if (!string.IsNullOrWhiteSpace(pageText))
				{
					allText.AppendLine(pageText);
					totalConfidence += 0.9; // PDF文本层通常置信度较高
					continue;
				}

				// 如果PDF页面是图像型，尝试从图像中提取文本
				// 对于图像型PDF，我们需要将页面渲染为图片后使用Google Vision API
				_logger.LogWarning("PDF page {PageNumber} appears to be image-based, attempting image OCR", pageCount);
				
				// 尝试将PDF页面转换为图片并OCR
				var pageImageBytes = ConvertPdfPageToImage(pdfBytes, pageCount - 1);
				if (pageImageBytes != null && pageImageBytes.Length > 0)
				{
					// 对图片进行OCR识别
					var imageOcrResult = RecognizeTextAsync(pageImageBytes, ct).Result;
					if (imageOcrResult != null && !string.IsNullOrWhiteSpace(imageOcrResult.Text))
					{
						allText.AppendLine(imageOcrResult.Text);
						totalConfidence += (double)imageOcrResult.Confidence;
					}
				}
			}

			if (allText.Length == 0)
			{
				_logger.LogWarning("No text extracted from PDF. The PDF may be corrupted or image-based.");
				return Task.FromResult<OcrResult?>(null);
			}

			var averageConfidence = pageCount > 0 ? totalConfidence / pageCount : 0;

			_logger.LogInformation("PDF OCR completed: {TextLength} characters from {PageCount} pages, Confidence: {Confidence}", 
				allText.Length, pageCount, averageConfidence);

			return Task.FromResult<OcrResult?>(new OcrResult
			{
				Text = allText.ToString(),
				Confidence = (decimal)averageConfidence,
				Pages = pageCount
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing PDF file");
			return Task.FromResult<OcrResult?>(null);
		}
	}

	/// <summary>
	/// 将PDF页面转换为图片（用于图像型PDF的OCR）
	/// </summary>
	private byte[]? ConvertPdfPageToImage(byte[] pdfBytes, int pageIndex)
	{
		try
		{
			// 使用SkiaSharp将PDF页面渲染为图片
			// 注意：这需要PDF渲染库，PdfPig本身不支持渲染
			// 这里先返回null，如果PDF是文本型的，上面的文本提取应该已经成功了
			// 如果PDF是图像型的，可能需要使用其他库如PdfiumViewer
			_logger.LogDebug("Attempting to convert PDF page {PageIndex} to image", pageIndex);
			return null; // 暂时返回null，优先使用文本提取
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error converting PDF page to image");
			return null;
		}
	}
}
