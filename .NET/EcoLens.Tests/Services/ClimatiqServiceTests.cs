using System.Net;
using System.Net.Http;
using System.Text.Json;
using EcoLens.Api.DTOs.Climatiq;
using EcoLens.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EcoLens.Tests.Services;

public class ClimatiqServiceTests
{
	private static IConfiguration CreateConfig(string? apiKey = "test-key", string? baseUrl = null)
	{
		var dict = new Dictionary<string, string>
		{
			["Climatiq:ApiKey"] = apiKey ?? "test-key",
			["Climatiq:BaseUrl"] = baseUrl ?? "https://api.climatiq.io"
		};
		return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
	}

	private static HttpClient CreateClient(HttpResponseMessage response)
	{
		var handler = new MockHttpMessageHandler(response);
		return new HttpClient(handler) { BaseAddress = new Uri("https://api.climatiq.io") };
	}

	private class MockHttpMessageHandler : HttpMessageHandler
	{
		private readonly HttpResponseMessage _response;

		public MockHttpMessageHandler(HttpResponseMessage response) => _response = response;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
			=> Task.FromResult(_response);
	}

	[Fact]
	public void Constructor_Throws_WhenApiKeyMissing()
	{
		var config = new ConfigurationBuilder().Build();
		var client = new HttpClient();
		Assert.Throws<ArgumentNullException>(() => new ClimatiqService(client, config));
	}

	[Fact]
	public async Task GetCarbonEmissionEstimateAsync_ReturnsDto_WhenSuccess()
	{
		var responseDto = new ClimatiqEstimateResponseDto { Co2e = 10.5m, Co2eUnit = "kg" };
		var json = JsonSerializer.Serialize(responseDto, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
		var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
		var client = CreateClient(response);
		var config = CreateConfig();
		var sut = new ClimatiqService(client, config);

		var result = await sut.GetCarbonEmissionEstimateAsync("activity-id", 1m, "kg", "US");

		Assert.NotNull(result);
		Assert.Equal(10.5m, result.Co2e);
		Assert.Equal("kg", result.Co2eUnit);
	}

	[Fact]
	public async Task GetCarbonEmissionEstimateAsync_Throws_WhenNotSuccess()
	{
		var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad request") };
		var client = CreateClient(response);
		var config = CreateConfig();
		var sut = new ClimatiqService(client, config);

		await Assert.ThrowsAsync<HttpRequestException>(() =>
			sut.GetCarbonEmissionEstimateAsync("activity-id", 1m, "kg", "US"));
	}
}
