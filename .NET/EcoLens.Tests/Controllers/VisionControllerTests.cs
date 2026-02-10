using System.Text;
using EcoLens.Api.Controllers;
using EcoLens.Api.DTOs;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EcoLens.Tests.Controllers;

public class VisionControllerTests
{
	private static IFormFile CreateFile(long length = 100)
	{
		var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake image"));
		return new FormFile(stream, 0, length, "file", "test.jpg")
		{
			Headers = new HeaderDictionary(),
			ContentType = "image/jpeg"
		};
	}

	[Fact]
	public async Task Analyze_ReturnsBadRequest_WhenFileNull()
	{
		var mockVision = new Mock<IVisionService>();
		var controller = new VisionController(mockVision.Object);
		var dto = new FileUploadDto { File = null! };

		var result = await controller.Analyze(dto, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("No image uploaded.", badRequest.Value);
		mockVision.Verify(x => x.PredictAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Analyze_ReturnsBadRequest_WhenFileLengthZero()
	{
		var mockVision = new Mock<IVisionService>();
		var controller = new VisionController(mockVision.Object);
		var dto = new FileUploadDto { File = CreateFile(0) };

		var result = await controller.Analyze(dto, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("No image uploaded.", badRequest.Value);
	}

	[Fact]
	public async Task Analyze_ReturnsOk_WhenSuccess()
	{
		var mockVision = new Mock<IVisionService>();
		var response = new VisionPredictionResponseDto
		{
			Label = "Fried Rice",
			Confidence = 0.95,
			SourceModel = "clip"
		};
		mockVision.Setup(x => x.PredictAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);
		var controller = new VisionController(mockVision.Object);
		var dto = new FileUploadDto { File = CreateFile() };

		var result = await controller.Analyze(dto, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var body = Assert.IsType<VisionPredictionResponseDto>(ok.Value);
		Assert.Equal("Fried Rice", body.Label);
		Assert.Equal(0.95, body.Confidence);
	}

	[Fact]
	public async Task Analyze_ReturnsBadRequest_WhenArgumentException()
	{
		var mockVision = new Mock<IVisionService>();
		mockVision.Setup(x => x.PredictAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new ArgumentException("Invalid format"));
		var controller = new VisionController(mockVision.Object);
		var dto = new FileUploadDto { File = CreateFile() };

		var result = await controller.Analyze(dto, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("Invalid format", badRequest.Value);
	}

	[Fact]
	public async Task Analyze_Returns500_WhenInvalidOperationException()
	{
		var mockVision = new Mock<IVisionService>();
		mockVision.Setup(x => x.PredictAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Service busy"));
		var controller = new VisionController(mockVision.Object);
		var dto = new FileUploadDto { File = CreateFile() };

		var result = await controller.Analyze(dto, CancellationToken.None);

		var status = Assert.IsType<ObjectResult>(result.Result);
		Assert.Equal(StatusCodes.Status500InternalServerError, status.StatusCode);
		Assert.Equal("Service busy", status.Value);
	}

	[Fact]
	public async Task Analyze_Returns502_WhenHttpRequestException()
	{
		var mockVision = new Mock<IVisionService>();
		mockVision.Setup(x => x.PredictAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new HttpRequestException("Connection refused"));
		var controller = new VisionController(mockVision.Object);
		var dto = new FileUploadDto { File = CreateFile() };

		var result = await controller.Analyze(dto, CancellationToken.None);

		var status = Assert.IsType<ObjectResult>(result.Result);
		Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
		Assert.Contains("Vision service error", status.Value?.ToString());
	}

	[Fact]
	public async Task Analyze_Returns500_WhenGenericException()
	{
		var mockVision = new Mock<IVisionService>();
		mockVision.Setup(x => x.PredictAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("Unexpected"));
		var controller = new VisionController(mockVision.Object);
		var dto = new FileUploadDto { File = CreateFile() };

		var result = await controller.Analyze(dto, CancellationToken.None);

		var status = Assert.IsType<ObjectResult>(result.Result);
		Assert.Equal(StatusCodes.Status500InternalServerError, status.StatusCode);
		Assert.Contains("Unexpected", status.Value?.ToString());
	}
}
