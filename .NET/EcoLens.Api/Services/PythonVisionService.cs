using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using EcoLens.Api.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace EcoLens.Api.Services;

public class PythonVisionService : IVisionService
{
	private readonly HttpClient _httpClient;
	private readonly VisionSettings _settings;
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};

	public PythonVisionService(HttpClient httpClient, IOptions<VisionSettings> options)
	{
		_httpClient = httpClient;
		_settings = options.Value;
	}

	public async Task<VisionPredictionResponseDto> PredictAsync(IFormFile image, CancellationToken cancellationToken)
	{
		if (image == null || image.Length == 0)
		{
			throw new ArgumentException("Uploaded image is invalid.", nameof(image));
		}

		var requestUri = "predict/image"; // baseAddress 已配置为 http://host:8000/

		using var content = new MultipartFormDataContent();
		await using var stream = image.OpenReadStream();
		var streamContent = new StreamContent(stream);
		streamContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType ?? "application/octet-stream");
		content.Add(streamContent, "file", image.FileName);

		using var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
		var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new HttpRequestException($"Python Vision Service call failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseText}");
		}

		var dto = JsonSerializer.Deserialize<VisionPredictionResponseDto>(responseText, SerializerOptions);
		if (dto == null)
		{
			throw new InvalidOperationException("Python Vision Service returned invalid data.");
		}
		return dto;
	}
}


