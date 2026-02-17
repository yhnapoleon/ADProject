using System.Net;
using System.Net.Http;
using System.Text.Json;
using EcoLens.Api.DTOs;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EcoLens.Tests.Services;

public class PythonVisionServiceTests
{
	private static IVisionService CreateService(HttpResponseMessage response)
	{
		var handler = new MockHttpHandler(response);
		var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000/") };
		var options = Options.Create(new VisionSettings { BaseUrl = "http://localhost:8000" });
		return new PythonVisionService(client, options);
	}

	private class MockHttpHandler : HttpMessageHandler
	{
		private readonly HttpResponseMessage _response;

		public MockHttpHandler(HttpResponseMessage response) => _response = response;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
			=> Task.FromResult(_response);
	}

	private static IFormFile CreateFile(long length, string fileName = "test.jpg", string contentType = "image/jpeg")
	{
		var mock = new Mock<IFormFile>();
		mock.Setup(f => f.Length).Returns(length);
		mock.Setup(f => f.FileName).Returns(fileName);
		mock.Setup(f => f.ContentType).Returns(contentType);
		mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
		return mock.Object;
	}

	[Fact]
	public async Task PredictAsync_Throws_WhenImageIsNull()
	{
		var response = new HttpResponseMessage(HttpStatusCode.OK);
		var sut = CreateService(response);
		await Assert.ThrowsAsync<ArgumentException>(() => sut.PredictAsync(null!, CancellationToken.None));
	}

	[Fact]
	public async Task PredictAsync_Throws_WhenImageLengthIsZero()
	{
		var response = new HttpResponseMessage(HttpStatusCode.OK);
		var sut = CreateService(response);
		var file = CreateFile(0);
		await Assert.ThrowsAsync<ArgumentException>(() => sut.PredictAsync(file, CancellationToken.None));
	}

	[Fact]
	public async Task PredictAsync_Throws_WhenHttpNotSuccess()
	{
		var response = new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("error") };
		var sut = CreateService(response);
		var file = CreateFile(100);
		await Assert.ThrowsAsync<HttpRequestException>(() => sut.PredictAsync(file, CancellationToken.None));
	}

	[Fact]
	public async Task PredictAsync_Throws_WhenResponseIsInvalidJson()
	{
		var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not json") };
		var sut = CreateService(response);
		var file = CreateFile(100);
		// Deserialize throws JsonException; service throws InvalidOperationException only when dto is null
		await Assert.ThrowsAsync<JsonException>(() => sut.PredictAsync(file, CancellationToken.None));
	}

	[Fact]
	public async Task PredictAsync_ReturnsDto_WhenSuccess()
	{
		var dto = new VisionPredictionResponseDto { Label = "Fried Rice", Confidence = 0.95 };
		var json = JsonSerializer.Serialize(dto);
		var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
		var sut = CreateService(response);
		var file = CreateFile(100);

		var result = await sut.PredictAsync(file, CancellationToken.None);

		Assert.NotNull(result);
		Assert.Equal("Fried Rice", result.Label);
		Assert.Equal(0.95, result.Confidence);
	}
}
