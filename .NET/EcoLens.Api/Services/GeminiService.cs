using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.DTOs;
using EcoLens.Api.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.Services;

public class GeminiService : IAiService
{
	private readonly HttpClient _httpClient;
	private readonly AiSettings _settings;
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public GeminiService(HttpClient httpClient, IOptions<AiSettings> options)
	{
		_httpClient = httpClient;
		_settings = options.Value;
	}

	public async Task<string> GetAnswerAsync(string userPrompt)
	{
		return await GetAnswerAsync(userPrompt, systemPrompt: null, ct: default);
	}

	/// <summary>
	/// 支持 System Prompt 的 Chat Completions 调用（OpenAI 兼容 messages 协议）。
	/// </summary>
	public async Task<string> GetAnswerAsync(string userPrompt, string? systemPrompt, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(userPrompt))
		{
			return string.Empty;
		}

		var baseUrl = _settings.BaseUrl?.TrimEnd('/') ?? string.Empty;
		if (string.IsNullOrWhiteSpace(baseUrl))
		{
			throw new InvalidOperationException("AiSettings.BaseUrl is not configured.");
		}

		if (string.IsNullOrWhiteSpace(_settings.ApiKey))
		{
			throw new InvalidOperationException("AiSettings.ApiKey is not configured.");
		}

		var targetUrl = $"{baseUrl}/chat/completions";

		using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

		var messages = new List<object>(capacity: 2);
		if (!string.IsNullOrWhiteSpace(systemPrompt))
		{
			messages.Add(new { role = "system", content = systemPrompt });
		}
		messages.Add(new { role = "user", content = userPrompt });

		var body = new
		{
			model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-3-flash-preview" : _settings.Model,
			messages = messages.ToArray()
		};

		var json = JsonSerializer.Serialize(body, SerializerOptions);
		request.Content = new StringContent(json, Encoding.UTF8, "application/json");

		using var response = await _httpClient.SendAsync(request, ct);
		var responseText = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			throw new HttpRequestException($"AI call failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseText}");
		}

		try
		{
			var parsed = JsonSerializer.Deserialize<ChatCompletionsResponse>(responseText, SerializerOptions);
			var content = parsed?.Choices?[0]?.Message?.Content ?? string.Empty;
			return content;
		}
		catch (JsonException)
		{
			// 响应非预期结构
			return string.Empty;
		}
	}

	public async Task<string> AnalyzeImageAsync(string prompt, IFormFile image)
	{
		if (image == null || image.Length == 0)
		{
			throw new ArgumentException("Uploaded image is empty.", nameof(image));
		}

		var baseUrl = _settings.BaseUrl?.TrimEnd('/') ?? string.Empty;
		if (string.IsNullOrWhiteSpace(baseUrl))
		{
			throw new InvalidOperationException("AiSettings.BaseUrl is not configured.");
		}

		if (string.IsNullOrWhiteSpace(_settings.ApiKey))
		{
			throw new InvalidOperationException("AiSettings.ApiKey is not configured.");
		}

		var contentType = string.IsNullOrWhiteSpace(image.ContentType) ? "image/jpeg" : image.ContentType;
		string base64;
		using (var ms = new MemoryStream())
		{
			await image.CopyToAsync(ms);
			base64 = Convert.ToBase64String(ms.ToArray());
		}

		// 简单判断当前是否为 OpenAI 兼容接口
		var isOpenAiCompatible = baseUrl.Contains("/v1", StringComparison.OrdinalIgnoreCase) ||
			baseUrl.Contains("openai", StringComparison.OrdinalIgnoreCase) ||
			baseUrl.Contains("openrouter", StringComparison.OrdinalIgnoreCase);

		if (isOpenAiCompatible)
		{
			var targetUrl = $"{baseUrl}/chat/completions";

			using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

			var dataUrl = $"data:{contentType};base64,{base64}";
			var body = new
			{
				model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-3-pro-preview" : _settings.Model,
				messages = new object[]
				{
					new
					{
						role = "user",
						content = new object[]
						{
							new { type = "text", text = prompt },
							new
							{
								type = "image_url",
								image_url = new { url = dataUrl }
							}
						}
					}
				}
			};

			var json = JsonSerializer.Serialize(body, SerializerOptions);
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");

			using var response = await _httpClient.SendAsync(request);
			var responseText = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				throw new HttpRequestException($"AI call failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseText}");
			}

			try
			{
				var parsed = JsonSerializer.Deserialize<ChatCompletionsResponse>(responseText, SerializerOptions);
				var content = parsed?.Choices?[0]?.Message?.Content ?? string.Empty;
				return content;
			}
			catch (JsonException)
			{
				return string.Empty;
			}
		}
		else
		{
			// 尝试 Google Gemini 原生接口：POST {baseUrl}/models/{model}:generateContent
			// 兼容常见 Gemini 部署（可能需要 ?key=xxx 或头部），这里优先使用 Bearer，如失败由上层感知
			var model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-1.5-pro" : _settings.Model;
			var targetUrl = $"{baseUrl}/models/{model}:generateContent";

			using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);
			// 部分网关仍使用 Bearer；如果后续接入原生 Gemini，需要改用 ?key= 查询参数或 x-goog-api-key
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

			var body = new
			{
				contents = new object[]
				{
					new
					{
						parts = new object[]
						{
							new { text = prompt },
							new { inline_data = new { mime_type = contentType, data = base64 } }
						}
					}
				}
			};

			var json = JsonSerializer.Serialize(body, SerializerOptions);
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");

			using var response = await _httpClient.SendAsync(request);
			var responseText = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				throw new HttpRequestException($"AI call failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseText}");
			}

			try
			{
				var parsed = JsonSerializer.Deserialize<GeminiResponse>(responseText, SerializerOptions);
				var text = parsed?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
				return text;
			}
			catch (JsonException)
			{
				return string.Empty;
			}
		}
	}

	private sealed class ChatCompletionsResponse
	{
		public Choice[]? Choices { get; set; }
	}

	private sealed class Choice
	{
		public Message? Message { get; set; }
	}

	private sealed class Message
	{
		public string? Content { get; set; }
	}

	private sealed class GeminiResponse
	{
		public GeminiCandidate[]? Candidates { get; set; }
	}

	private sealed class GeminiCandidate
	{
		public GeminiContent? Content { get; set; }
	}

	private sealed class GeminiContent
	{
		public GeminiPart[]? Parts { get; set; }
	}

	private sealed class GeminiPart
	{
		public string? Text { get; set; }
	}
}


