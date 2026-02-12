using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.DTOs;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace EcoLens.Tests;

public class GeminiServiceTests
{
	private class DummyHandler : HttpMessageHandler
	{
		private readonly HttpStatusCode _statusCode;
		private readonly string _responseBody;

		public DummyHandler(HttpStatusCode statusCode, string responseBody)
		{
			_statusCode = statusCode;
			_responseBody = responseBody;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var response = new HttpResponseMessage(_statusCode)
			{
				Content = new StringContent(_responseBody)
			};
			return Task.FromResult(response);
		}
	}

	private class FakeFormFile : IFormFile
	{
		private readonly byte[] _content;

		public FakeFormFile(string name, string fileName, string contentType, byte[] content)
		{
			Name = name;
			FileName = fileName;
			ContentType = contentType;
			_content = content ?? Array.Empty<byte>();
		}

		public string ContentType { get; }
		public string ContentDisposition { get; set; } = string.Empty;
		public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
		public long Length => _content.Length;
		public string Name { get; }
		public string FileName { get; }

		public void CopyTo(System.IO.Stream target) => target.Write(_content, 0, _content.Length);
		public Task CopyToAsync(System.IO.Stream target, CancellationToken cancellationToken = default)
		{
			target.Write(_content, 0, _content.Length);
			return Task.CompletedTask;
		}
		public System.IO.Stream OpenReadStream() => new System.IO.MemoryStream(_content);
	}

	private static GeminiService CreateService(HttpMessageHandler handler, AiSettings settings)
	{
		var client = new HttpClient(handler);
		var options = Options.Create(settings);
		return new GeminiService(client, options);
	}

	[Fact]
	public async Task GetAnswerAsync_ShouldReturnEmpty_WhenPromptIsWhitespace()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, "{}"),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "key" });

		var result = await service.GetAnswerAsync("   ");

		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public async Task GetAnswerAsync_ShouldThrow_WhenBaseUrlMissing()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, "{}"),
			new AiSettings { BaseUrl = "", ApiKey = "key" });

		await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAnswerAsync("hello"));
	}

	[Fact]
	public async Task AnalyzeImageAsync_ShouldThrowArgumentException_WhenImageEmpty()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, "{}"),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "key" });

		var empty = new FakeFormFile("file", "img.jpg", "image/jpeg", Array.Empty<byte>());

		await Assert.ThrowsAsync<ArgumentException>(() => service.AnalyzeImageAsync("prompt", empty));
	}

	[Fact]
	public async Task AnalyzeImageAsync_ShouldThrow_WhenBaseUrlMissing()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, "{}"),
			new AiSettings { BaseUrl = "", ApiKey = "key" });

		var file = new FakeFormFile("file", "img.jpg", "image/jpeg", new byte[] { 1, 2, 3 });

		await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeImageAsync("prompt", file));
	}

	[Fact]
	public async Task GetAnswerAsync_ShouldReturnEmpty_WhenPromptIsNull()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, "{}"),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "key" });

		var result = await service.GetAnswerAsync(null!);

		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public async Task GetAnswerAsync_ShouldThrow_WhenApiKeyMissing()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, "{}"),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "" });

		await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAnswerAsync("hello"));
	}

	[Fact]
	public async Task GetAnswerAsync_ShouldReturnContent_WhenSuccess()
	{
		var json = """{"choices":[{"message":{"content":"Hi there"}}]}""";
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, json),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "key" });

		var result = await service.GetAnswerAsync("hello");

		Assert.Equal("Hi there", result);
	}

	[Fact]
	public async Task GetAnswerAsync_ShouldThrowHttpRequestException_WhenNotSuccessStatusCode()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.ServiceUnavailable, "error body"),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "key" });

		var ex = await Assert.ThrowsAsync<HttpRequestException>(() => service.GetAnswerAsync("hello"));
		Assert.Contains("503", ex.Message);
		Assert.Contains("error body", ex.Message);
	}

	[Fact]
	public async Task GetAnswerAsync_ShouldReturnEmpty_WhenResponseIsInvalidJson()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, "not json at all"),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "key" });

		var result = await service.GetAnswerAsync("hello");

		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public async Task GetAnswerAsync_ShouldUseDefaultModel_WhenModelEmpty()
	{
		var json = """{"choices":[{"message":{"content":"ok"}}]}""";
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, json),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "key", Model = "" });

		var result = await service.GetAnswerAsync("hi");
		Assert.Equal("ok", result);
	}

	[Fact]
	public async Task AnalyzeImageAsync_ShouldThrow_WhenImageNull()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, "{}"),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "key" });

		await Assert.ThrowsAsync<ArgumentException>(() => service.AnalyzeImageAsync("prompt", null!));
	}

	[Fact]
	public async Task AnalyzeImageAsync_ShouldThrow_WhenApiKeyMissing()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, "{}"),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "" });

		var file = new FakeFormFile("file", "img.jpg", "image/jpeg", new byte[] { 1, 2, 3 });
		await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeImageAsync("prompt", file));
	}

	[Fact]
	public async Task AnalyzeImageAsync_OpenAiPath_ShouldReturnContent_WhenSuccess()
	{
		var json = """{"choices":[{"message":{"content":"image analysis result"}}]}""";
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, json),
			new AiSettings { BaseUrl = "https://api.openai.com/v1", ApiKey = "key" });

		var file = new FakeFormFile("file", "img.jpg", "image/jpeg", new byte[] { 1, 2, 3 });
		var result = await service.AnalyzeImageAsync("describe", file);

		Assert.Equal("image analysis result", result);
	}

	[Fact]
	public async Task AnalyzeImageAsync_GeminiPath_ShouldReturnContent_WhenSuccess()
	{
		var json = """{"candidates":[{"content":{"parts":[{"text":"gemini vision result"}]}}]}""";
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, json),
			new AiSettings { BaseUrl = "https://generativelanguage.googleapis.com", ApiKey = "key" });

		var file = new FakeFormFile("file", "img.jpg", "image/jpeg", new byte[] { 1, 2, 3 });
		var result = await service.AnalyzeImageAsync("describe", file);

		Assert.Equal("gemini vision result", result);
	}

	[Fact]
	public async Task AnalyzeImageAsync_ShouldThrowHttpRequestException_WhenNotSuccessStatusCode()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.BadRequest, "bad request"),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "key" });

		var file = new FakeFormFile("file", "img.jpg", "image/jpeg", new byte[] { 1, 2, 3 });
		var ex = await Assert.ThrowsAsync<HttpRequestException>(() => service.AnalyzeImageAsync("prompt", file));
		Assert.Contains("400", ex.Message);
	}

	[Fact]
	public async Task AnalyzeImageAsync_OpenAiPath_ShouldReturnEmpty_WhenInvalidJson()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, "invalid"),
			new AiSettings { BaseUrl = "https://api.openrouter.ai/v1", ApiKey = "key" });

		var file = new FakeFormFile("file", "img.jpg", "image/jpeg", new byte[] { 1, 2, 3 });
		var result = await service.AnalyzeImageAsync("prompt", file);

		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public async Task AnalyzeImageAsync_GeminiPath_ShouldReturnEmpty_WhenInvalidJson()
	{
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, "{}"),
			new AiSettings { BaseUrl = "https://gemini.example.com", ApiKey = "key" });

		var file = new FakeFormFile("file", "img.jpg", "image/jpeg", new byte[] { 1, 2, 3 });
		var result = await service.AnalyzeImageAsync("prompt", file);

		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public async Task AnalyzeImageAsync_ShouldUseDefaultContentType_WhenContentTypeEmpty()
	{
		var json = """{"choices":[{"message":{"content":"ok"}}]}""";
		var service = CreateService(
			new DummyHandler(HttpStatusCode.OK, json),
			new AiSettings { BaseUrl = "https://api.example.com/v1", ApiKey = "key" });

		var file = new FakeFormFile("file", "img.jpg", "", new byte[] { 1, 2, 3 });
		var result = await service.AnalyzeImageAsync("prompt", file);

		Assert.Equal("ok", result);
	}
}

