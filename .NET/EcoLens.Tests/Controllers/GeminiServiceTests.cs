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
}

