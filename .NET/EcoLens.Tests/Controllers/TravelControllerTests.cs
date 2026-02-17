using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Controllers;
using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EcoLens.Tests;

	public class TravelControllerTests
	{
		private class FakeTravelService : ITravelService
		{
			public int LastCreateUserId { get; private set; }
			public CreateTravelLogDto? LastCreateDto { get; private set; }
			public bool CreateCalled { get; private set; }
			public bool PreviewCalled { get; private set; }
			public bool GetLogsCalled { get; private set; }

			public int LastGetByIdUserId { get; private set; }
			public int LastGetByIdId { get; private set; }
			public TravelLogResponseDto? GetByIdResult { get; set; }

			public int LastDeleteUserId { get; private set; }
			public int LastDeleteId { get; private set; }
			public bool DeleteResult { get; set; }

			public Task<TravelLogResponseDto> CreateTravelLogAsync(int userId, CreateTravelLogDto dto, CancellationToken ct = default)
			{
				CreateCalled = true;
				LastCreateUserId = userId;
				LastCreateDto = dto;

				return Task.FromResult(new TravelLogResponseDto
				{
					Id = 1,
					CreatedAt = DateTime.UtcNow,
					TransportMode = dto.TransportMode,
					TransportModeName = "Bus",
					OriginAddress = dto.OriginAddress,
					DestinationAddress = dto.DestinationAddress,
					DistanceMeters = 1000,
					DistanceKilometers = 1,
					CarbonEmission = 10
				});
			}

			public Task<RoutePreviewDto> PreviewRouteAsync(CreateTravelLogDto dto, CancellationToken ct = default)
			{
				PreviewCalled = true;
				return Task.FromResult(new RoutePreviewDto
				{
					OriginAddress = dto.OriginAddress,
					DestinationAddress = dto.DestinationAddress,
					TransportMode = dto.TransportMode,
					TransportModeName = "Bus",
					DistanceMeters = 1000,
					DistanceKilometers = 1,
					EstimatedCarbonEmission = 10
				});
			}

			public Task<PagedResultDto<TravelLogResponseDto>> GetUserTravelLogsAsync(int userId, GetTravelLogsQueryDto? query = null, CancellationToken ct = default)
			{
				GetLogsCalled = true;
				return Task.FromResult(new PagedResultDto<TravelLogResponseDto>
				{
					Items = new List<TravelLogResponseDto>(),
					TotalCount = 0,
					Page = query?.Page ?? 1,
					PageSize = query?.PageSize ?? 20
				});
			}

			public Task<TravelLogResponseDto?> GetTravelLogByIdAsync(int id, int userId, CancellationToken ct = default)
			{
				LastGetByIdId = id;
				LastGetByIdUserId = userId;
				return Task.FromResult<TravelLogResponseDto?>(GetByIdResult);
			}

			public Task<bool> DeleteTravelLogAsync(int id, int userId, CancellationToken ct = default)
			{
				LastDeleteId = id;
				LastDeleteUserId = userId;
				return Task.FromResult(DeleteResult);
			}

			public Task<TravelStatisticsDto> GetUserTravelStatisticsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
				=> Task.FromResult(new TravelStatisticsDto());
		}

    private class FakePointService : IPointService
    {
        public int CheckCalledUserId { get; private set; }
        public DateTime CheckCalledDate { get; private set; }
        public bool RecalculateCalled { get; private set; }

        public Task CheckAndAwardPointsAsync(int userId, DateTime date)
        {
            CheckCalledUserId = userId;
            CheckCalledDate = date;
            return Task.CompletedTask;
        }

        public Task AwardTreePlantingPointsAsync(int userId, int treesPlantedCount)
            => Task.CompletedTask;

        public Task<decimal> CalculateDailyNetValueAsync(int userId, DateTime date)
            => Task.FromResult(0m);

        public Task RecalculateTotalCarbonSavedAsync(int userId)
        {
            RecalculateCalled = true;
            return Task.CompletedTask;
        }

        public Task LogPointAwardAsync(int userId, int points, DateTime awardedAt, string source)
            => Task.CompletedTask;
    }

    private static TravelController CreateControllerWithUser(FakeTravelService travelService, FakePointService pointService, int? userId)
    {
        var logger = new LoggerFactory().CreateLogger<TravelController>();
        var controller = new TravelController(travelService, pointService, logger);

        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            }, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenUserIdMissing()
    {
        var travelService = new FakeTravelService();
        var pointService = new FakePointService();
        var controller = CreateControllerWithUser(travelService, pointService, userId: null);

        var dto = new CreateTravelLogDto
        {
            OriginAddress = "A",
            DestinationAddress = "B",
            TransportMode = TransportMode.Bus
        };

        var result = await controller.Create(dto, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Contains("Unable to get user information", unauthorized.Value!.ToString());
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenModelStateInvalid()
    {
        var travelService = new FakeTravelService();
        var pointService = new FakePointService();
        var controller = CreateControllerWithUser(travelService, pointService, userId: 1);
        controller.ModelState.AddModelError("OriginAddress", "Required");

        var dto = new CreateTravelLogDto(); // 缺少必填字段

        var result = await controller.Create(dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Request validation failed", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task Create_ShouldCallServices_AndReturnOk_OnSuccess()
    {
        var travelService = new FakeTravelService();
        var pointService = new FakePointService();
        var controller = CreateControllerWithUser(travelService, pointService, userId: 42);

        var dto = new CreateTravelLogDto
        {
            OriginAddress = "A",
            DestinationAddress = "B",
            TransportMode = TransportMode.Bus
        };

        var result = await controller.Create(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<TravelLogResponseDto>(ok.Value);
        Assert.Equal("A", body.OriginAddress);
        Assert.Equal("B", body.DestinationAddress);

        Assert.True(travelService.CreateCalled);
        Assert.Equal(42, travelService.LastCreateUserId);
        Assert.Equal(TransportMode.Bus, travelService.LastCreateDto!.TransportMode);

        Assert.Equal(42, pointService.CheckCalledUserId);
        Assert.True(pointService.RecalculateCalled);
    }

    [Fact]
    public async Task Preview_ShouldReturnUnauthorized_WhenUserIdMissing()
    {
        var travelService = new FakeTravelService();
        var pointService = new FakePointService();
        var controller = CreateControllerWithUser(travelService, pointService, userId: null);

        var dto = new CreateTravelLogDto
        {
            OriginAddress = "A",
            DestinationAddress = "B",
            TransportMode = TransportMode.Bus
        };

        var result = await controller.Preview(dto, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Contains("Unable to get user information", unauthorized.Value!.ToString());
    }

    [Fact]
    public async Task GetMyTravels_ShouldReturnUnauthorized_WhenUserIdMissing()
    {
        var travelService = new FakeTravelService();
        var pointService = new FakePointService();
        var controller = CreateControllerWithUser(travelService, pointService, userId: null);

        var query = new GetTravelLogsQueryDto();

        var result = await controller.GetMyTravels(query, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Contains("Unable to get user information", unauthorized.Value!.ToString());
    }

	[Fact]
	public async Task Delete_ShouldReturnUnauthorized_WhenUserIdMissing()
	{
		var travelService = new FakeTravelService();
		var pointService = new FakePointService();
		var controller = CreateControllerWithUser(travelService, pointService, userId: null);

		var result = await controller.Delete(10, CancellationToken.None);

		var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
		Assert.Contains("Unable to get user information", unauthorized.Value!.ToString());
	}

	[Fact]
	public async Task Delete_ShouldReturnNotFound_WhenServiceReturnsFalse()
	{
		var travelService = new FakeTravelService
		{
			DeleteResult = false
		};
		var pointService = new FakePointService();
		var controller = CreateControllerWithUser(travelService, pointService, userId: 5);

		var result = await controller.Delete(99, CancellationToken.None);

		var notFound = Assert.IsType<NotFoundObjectResult>(result);
		Assert.Contains("Travel log not found or access denied", notFound.Value!.ToString());
		Assert.Equal(99, travelService.LastDeleteId);
		Assert.Equal(5, travelService.LastDeleteUserId);
	}

	[Fact]
	public async Task Delete_ShouldReturnOk_WhenDeleted()
	{
		var travelService = new FakeTravelService
		{
			DeleteResult = true
		};
		var pointService = new FakePointService();
		var controller = CreateControllerWithUser(travelService, pointService, userId: 7);

		var result = await controller.Delete(123, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Contains("Deleted successfully", ok.Value!.ToString());
		Assert.Equal(123, travelService.LastDeleteId);
		Assert.Equal(7, travelService.LastDeleteUserId);
	}
}

