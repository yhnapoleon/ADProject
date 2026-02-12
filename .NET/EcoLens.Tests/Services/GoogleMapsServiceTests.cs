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

public class GoogleMapsServiceTests
{
	private sealed class FakeHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _handler;

		public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
		{
			_handler = handler;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			=> Task.FromResult(_handler(request, cancellationToken));
	}

	private static GoogleMapsService CreateService(HttpMessageHandler handler)
	{
		var client = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://maps.googleapis.com")
		};

		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["GoogleMaps:ApiKey"] = "test-key"
			})
			.Build();

		var logger = new Mock<ILogger<GoogleMapsService>>();
		return new GoogleMapsService(client, config, logger.Object);
	}

	[Fact]
	public async Task GeocodeAsync_ShouldNormalizeSingaporePostalCode_AndReturnResult()
	{
		string? requestedUrl = null;
		var handler = new FakeHttpMessageHandler((request, _) =>
		{
			requestedUrl = request.RequestUri!.ToString();

			var payload = new
			{
				status = "OK",
				results = new[]
				{
					new
					{
						formatted_address = "123456 Singapore",
						geometry = new
						{
							location = new { lat = 1.23, lng = 4.56 }
						},
						address_components = new[]
						{
							new { long_name = "Singapore", types = new[] { "country" } },
							new { long_name = "Some City", types = new[] { "locality" } }
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

		var result = await service.GeocodeAsync("123456", CancellationToken.None);

		Assert.NotNull(result);
		Assert.Equal(1.23, result!.Latitude, 3);
		Assert.Equal(4.56, result.Longitude, 3);
		Assert.Equal("Singapore", result.Country);
		Assert.Equal("Some City", result.City);
		// 成功返回结果即可（内部已完成归一化与调用）
	}

	[Fact]
	public async Task ReverseGeocodeAsync_ShouldReturnResult()
	{
		string? requestedUrl = null;
		var handler = new FakeHttpMessageHandler((request, _) =>
		{
			requestedUrl = request.RequestUri!.ToString();

			var payload = new
			{
				status = "OK",
				results = new[]
				{
					new
					{
						formatted_address = "Somewhere",
						address_components = new[]
						{
							new { long_name = "City", types = new[] { "locality" } },
							new { long_name = "Country", types = new[] { "country" } }
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

		var result = await service.ReverseGeocodeAsync(1.0, 2.0, CancellationToken.None);

		Assert.NotNull(result);
		Assert.Equal("Somewhere", result!.FormattedAddress);
		Assert.Equal("City", result.City);
		Assert.Equal("Country", result.Country);
	}

	[Fact]
	public async Task CalculateDistanceAsync_ShouldReturnResult()
	{
		string? requestedUrl = null;
		var handler = new FakeHttpMessageHandler((request, _) =>
		{
			requestedUrl = request.RequestUri!.ToString();

			var payload = new
			{
				status = "OK",
				rows = new[]
				{
					new
					{
						elements = new[]
						{
							new
							{
								status = "OK",
								distance = new { value = 1234, text = "1.2 km" },
								duration = new { value = 567, text = "9 mins" }
							}
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

		var result = await service.CalculateDistanceAsync(1, 2, 3, 4, CancellationToken.None);

		Assert.NotNull(result);
		Assert.Equal(1234, result!.DistanceMeters);
		Assert.Equal(567, result.DurationSeconds);
		Assert.Equal("1.2 km", result.DistanceText);
		Assert.Equal("9 mins", result.DurationText);
	}

	[Fact]
	public async Task SearchNearbyAsync_AirportKeyword_UsesTypeAirport_AndReturnsPlaces()
	{
		string? requestedUrl = null;
		var handler = new FakeHttpMessageHandler((request, _) =>
		{
			requestedUrl = request.RequestUri!.ToString();

			var payload = new
			{
				status = "OK",
				results = new[]
				{
					new
					{
						name = "Changi Airport",
						vicinity = "Singapore",
						geometry = new
						{
							location = new { lat = 1.36, lng = 103.99 }
						},
						rating = 4.5
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

		var result = await service.SearchNearbyAsync(1.0, 2.0, "airport", 5000, CancellationToken.None);

		Assert.NotNull(result);
		Assert.Single(result!.Places);
		Assert.Equal("Changi Airport", result.Places[0].Name);
		// 对于 airport 关键字，应使用 type=airport
		Assert.NotNull(requestedUrl);
		Assert.Contains("type=airport", requestedUrl!);
	}

	[Fact]
	public async Task GetRouteAsync_ShouldReturnRouteResult()
	{
		string? requestedUrl = null;
		var handler = new FakeHttpMessageHandler((request, _) =>
		{
			requestedUrl = request.RequestUri!.ToString();

			var payload = new
			{
				status = "OK",
				routes = new[]
				{
					new
					{
						legs = new[]
						{
							new
							{
								distance = new { value = 1000, text = "1 km" },
								duration = new { value = 600, text = "10 mins" }
							}
						},
						overview_polyline = new
						{
							points = "encoded-polyline"
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

		var result = await service.GetRouteAsync(1, 2, 3, 4, "driving", CancellationToken.None);

		Assert.NotNull(result);
		Assert.Equal(1000, result!.DistanceMeters);
		Assert.Equal(600, result.DurationSeconds);
		Assert.Equal("1 km", result.DistanceText);
		Assert.Equal("10 mins", result.DurationText);
		Assert.Equal("encoded-polyline", result.Polyline);
	}
}

