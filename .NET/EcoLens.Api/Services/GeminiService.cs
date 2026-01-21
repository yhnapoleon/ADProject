using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EcoLens.Api.DTOs;
using Microsoft.Extensions.Options;

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
		if (string.IsNullOrWhiteSpace(userPrompt))
		{
			return string.Empty;
		}

		var baseUrl = _settings.BaseUrl?.TrimEnd('/') ?? string.Empty;
		if (string.IsNullOrWhiteSpace(baseUrl))
		{
			throw new InvalidOperationException("AiSettings.BaseUrl 未配置。");
		}

		if (string.IsNullOrWhiteSpace(_settings.ApiKey))
		{
			throw new InvalidOperationException("AiSettings.ApiKey 未配置。");
		}

		var targetUrl = $"{baseUrl}/chat/completions";

		using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

		var body = new
		{
			model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-3-pro-preview" : _settings.Model,
			messages = new object[]
			{
				new { role = "user", content = userPrompt }
			}
		};

		var json = JsonSerializer.Serialize(body, SerializerOptions);
		request.Content = new StringContent(json, Encoding.UTF8, "application/json");

		using var response = await _httpClient.SendAsync(request);
		var responseText = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			throw new HttpRequestException($"AI 调用失败: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseText}");
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
}


