using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EcoLens.Tests.Services;

public class OcrServiceTests
{
	private sealed class FakeHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _handler;

		public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
		{
			_handler = handler;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return Task.FromResult(_handler(request, cancellationToken));
		}
	}

	private static OcrService CreateService(HttpMessageHandler handler)
	{
		var client = new HttpClient(handler);
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["GoogleCloud:VisionApiKey"] = "test-key"
			})
			.Build();

		var logger = new Mock<ILogger<OcrService>>();
		return new OcrService(client, config, logger.Object);
	}

	[Fact]
	public async Task RecognizeTextAsync_ReturnsResult_WhenApiSucceeds()
	{
		var bytes = Encoding.UTF8.GetBytes("image-bytes");

		var handler = new FakeHttpMessageHandler((_, _) =>
		{
			var payload = new
			{
				responses = new[]
				{
					new
					{
						fullTextAnnotation = new
						{
							text = "Hello OCR",
							pages = new[]
							{
								new { confidence = 0.8 }
							}
						},
						error = (object?)null
					}
				}
			};

			var json = JsonSerializer.Serialize(payload);
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
		});

		var service = CreateService(handler);

		var result = await service.RecognizeTextAsync(bytes, CancellationToken.None);

		Assert.NotNull(result);
		Assert.Equal("Hello OCR", result!.Text);
		Assert.Equal(0.8m, result.Confidence);
		Assert.Equal(1, result.Pages);
	}

	[Fact]
	public async Task RecognizeTextAsync_ReturnsNull_WhenApiReturnsNonSuccess()
	{
		var bytes = Encoding.UTF8.GetBytes("image-bytes");

		var handler = new FakeHttpMessageHandler((_, _) =>
		{
			return new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				Content = new StringContent("{\"error\":\"bad request\"}", Encoding.UTF8, "application/json")
			};
		});

		var service = CreateService(handler);

		var result = await service.RecognizeTextAsync(bytes, CancellationToken.None);

		Assert.Null(result);
	}

	[Fact]
	public async Task RecognizeTextAsync_ReturnsNull_WhenVisionResponseHasError()
	{
		var bytes = Encoding.UTF8.GetBytes("image-bytes");

		var handler = new FakeHttpMessageHandler((_, _) =>
		{
			var payload = new
			{
				responses = new[]
				{
					new
					{
						fullTextAnnotation = (object?)null,
						error = new
						{
							code = 13,
							message = "internal error"
						}
					}
				}
			};

			var json = JsonSerializer.Serialize(payload);
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
		});

		var service = CreateService(handler);

		var result = await service.RecognizeTextAsync(bytes, CancellationToken.None);

		Assert.Null(result);
	}

	[Fact]
	public async Task RecognizeTextAsync_SkipsCall_WhenImageTooLarge()
	{
		// 大于 20MB 的图片
		var bytes = new byte[20 * 1024 * 1024 + 1];
		var called = false;

		var handler = new FakeHttpMessageHandler((_, _) =>
		{
			called = true;
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		var service = CreateService(handler);

		var result = await service.RecognizeTextAsync(bytes, CancellationToken.None);

		Assert.Null(result);
		Assert.False(called); // 不应调用外部 API
	}
}

