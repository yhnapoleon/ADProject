using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

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
}
