using EcoLens.Api.Controllers;
using EcoLens.Api.DTOs.Trip;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EcoLens.Tests.Controllers;

public class TripControllerTests
{
	[Fact]
	public async Task Calculate_ReturnsValidationProblem_WhenModelInvalid()
	{
		var controller = new TripController();
		controller.ModelState.AddModelError("StartLocation", "Required");

		var result = await controller.Calculate(new TripCalculateRequestDto
		{
			StartLocation = "A",
			EndLocation = "B",
			TransportMode = TransportMode.Walking
		}, CancellationToken.None);

		Assert.False(result.Result is OkObjectResult);
	}

	[Fact]
	public async Task Calculate_ReturnsOkWithEmissionZero_WhenWalking()
	{
		var controller = new TripController();
		var dto = new TripCalculateRequestDto
		{
			StartLocation = "Orchard",
			EndLocation = "Marina",
			TransportMode = TransportMode.Walking
		};

		var result = await controller.Calculate(dto, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var body = Assert.IsType<TripCalculateResponseDto>(ok.Value);
		Assert.Equal(TransportMode.Walking, body.TransportMode);
		Assert.Equal(0m, body.EstimatedEmission);
		Assert.InRange(body.DistanceKm, 1.0, 50.0);
	}

	[Fact]
	public async Task Calculate_ReturnsOkWithEmission_WhenCarGasoline()
	{
		var controller = new TripController();
		var dto = new TripCalculateRequestDto
		{
			StartLocation = "A",
			EndLocation = "B",
			TransportMode = TransportMode.CarGasoline
		};

		var result = await controller.Calculate(dto, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var body = Assert.IsType<TripCalculateResponseDto>(ok.Value);
		Assert.Equal(TransportMode.CarGasoline, body.TransportMode);
		Assert.True(body.EstimatedEmission >= 0.21m && body.EstimatedEmission <= 50m * 0.21m);
		Assert.InRange(body.DistanceKm, 1.0, 50.0);
	}

	[Fact]
	public async Task CalculateAlias_DelegatesToCalculate()
	{
		var controller = new TripController();
		var dto = new TripCalculateRequestDto
		{
			StartLocation = "X",
			EndLocation = "Y",
			TransportMode = TransportMode.Subway
		};

		var result = await controller.CalculateAlias(dto, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var body = Assert.IsType<TripCalculateResponseDto>(ok.Value);
		Assert.Equal(TransportMode.Subway, body.TransportMode);
		Assert.InRange(body.DistanceKm, 1.0, 50.0);
	}
}
