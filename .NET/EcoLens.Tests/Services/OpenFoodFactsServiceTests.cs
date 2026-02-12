using System.Net;
using System.Net.Http;
using System.Text.Json;
using EcoLens.Api.DTOs.OpenFoodFacts;
using EcoLens.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EcoLens.Tests.Services;

public class OpenFoodFactsServiceTests
{
	private static IConfiguration CreateConfig(string? baseUrl = null)
	{
		var dict = new Dictionary<string, string>
		{
			["OpenFoodFacts:BaseUrl"] = baseUrl ?? "https://world.openfoodfacts.org/api/v2/product/"
		};
		return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
	}

	private static HttpClient CreateClient(HttpResponseMessage response, string? requestedUri = null)
	{
		var handler = new MockHttpHandler(response, requestedUri);
		return new HttpClient(handler) { BaseAddress = new Uri("https://world.openfoodfacts.org/api/v2/product/") };
	}

	private class MockHttpHandler : HttpMessageHandler
	{
		private readonly HttpResponseMessage _response;
		private readonly string? _captureUri;

		public MockHttpHandler(HttpResponseMessage response, string? captureUri = null)
		{
			_response = response;
			_captureUri = captureUri;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
			=> Task.FromResult(_response);
	}

	[Fact]
	public async Task GetProductByBarcodeAsync_ReturnsNull_WhenNotFound()
	{
		var response = new HttpResponseMessage(HttpStatusCode.NotFound);
		var client = CreateClient(response);
		var config = CreateConfig();
		var sut = new OpenFoodFactsService(client, config);

		var result = await sut.GetProductByBarcodeAsync("0000000000000");

		Assert.Null(result);
	}

	[Fact]
	public async Task GetProductByBarcodeAsync_ReturnsDto_WhenSuccess_WithCo2InEcoScore()
	{
		var dto = new OpenFoodFactsProductResponseDto
		{
			Code = "123",
			Status = 1,
			Product = new ProductDto
			{
				ProductName = "Test",
				EcoScoreData = new EcoScoreDataDto
				{
					Agribalyse = new AgribalyseDataDto { Co2Total = 2.5m }
				}
			}
		};
		var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, WriteIndented = false };
		var json = JsonSerializer.Serialize(dto, options);
		var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
		var client = CreateClient(response);
		var config = CreateConfig();
		var sut = new OpenFoodFactsService(client, config);

		var result = await sut.GetProductByBarcodeAsync("123");

		Assert.NotNull(result);
		Assert.Equal(2.5m, result.Product!.EcoScoreData!.Agribalyse!.Co2Total);
	}

	[Fact]
	public async Task GetProductByBarcodeAsync_PatchesCo2_WhenEcoScoreIsStringInRawJson()
	{
		// Raw JSON with ecoscore_data as string (nested JSON) so TryPatchEcoScoreFromRaw is used
		var raw = """{"product":{"product_name":"Beef","ecoscore_data":"{\"agribalyse\":{\"co2_total\":3.0}}"}}""";
		var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(raw) };
		var client = CreateClient(response);
		var config = CreateConfig();
		var sut = new OpenFoodFactsService(client, config);

		var result = await sut.GetProductByBarcodeAsync("789");

		Assert.NotNull(result?.Product?.EcoScoreData);
		Assert.NotNull(result.Product.EcoScoreData.Agribalyse);
		Assert.Equal(3.0m, result.Product.EcoScoreData.Agribalyse.Co2Total);
	}

	[Fact]
	public async Task GetProductByBarcodeAsync_DeserializesProduct_WhenEcoScoreIsObject()
	{
		// ecoscore_data as object with agribalyse.co2_total
		var raw = """{"code":"456","status":1,"product":{"product_name":"Fish","ecoscore_data":{"agribalyse":{"co2_total":4.5}}}}""";
		var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(raw) };
		var client = CreateClient(response);
		var config = CreateConfig();
		var sut = new OpenFoodFactsService(client, config);

		var result = await sut.GetProductByBarcodeAsync("456");

		Assert.NotNull(result?.Product);
		Assert.Equal("Fish", result.Product.ProductName);
		Assert.NotNull(result.Product.EcoScoreData?.Agribalyse);
		Assert.Equal(4.5m, result.Product.EcoScoreData.Agribalyse.Co2Total);
	}
}
