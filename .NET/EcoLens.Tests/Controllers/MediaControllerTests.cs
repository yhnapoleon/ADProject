using System.Text;
using EcoLens.Api.Controllers;
using EcoLens.Api.DTOs.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EcoLens.Tests.Controllers;

public class MediaControllerTests
{
	private static IFormFile CreateFormFile(string fileName, long length = 100)
	{
		var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake image content"));
		return new FormFile(stream, 0, length, "file", fileName)
		{
			Headers = new HeaderDictionary(),
			ContentType = "image/jpeg"
		};
	}

	[Fact]
	public async Task Upload_ReturnsBadRequest_WhenFileNull()
	{
		var controller = new MediaController();

		var result = await controller.Upload(null!, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("No file uploaded.", badRequest.Value);
	}

	[Fact]
	public async Task Upload_ReturnsBadRequest_WhenFileEmpty()
	{
		var controller = new MediaController();
		var file = CreateFormFile("test.jpg", 0);

		var result = await controller.Upload(file, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("No file uploaded.", badRequest.Value);
	}

	[Fact]
	public async Task Upload_ReturnsBadRequest_WhenExtensionNotAllowed()
	{
		var controller = new MediaController();
		var file = CreateFormFile("doc.pdf", 100);

		var result = await controller.Upload(file, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("Only .jpg/.jpeg/.png files are allowed.", badRequest.Value);
	}

	[Fact]
	public async Task Upload_ReturnsOkWithUrl_WhenFileValid()
	{
		var controller = new MediaController();
		var file = CreateFormFile("photo.jpg", 100);
		var originalDir = Directory.GetCurrentDirectory();
		var tempRoot = Path.Combine(Path.GetTempPath(), "EcoLensTests", Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(tempRoot);
			Directory.SetCurrentDirectory(tempRoot);

			var result = await controller.Upload(file, CancellationToken.None);

			var ok = Assert.IsType<OkObjectResult>(result.Result);
			var dto = Assert.IsType<UploadResponseDto>(ok.Value);
			Assert.NotNull(dto.Url);
			Assert.StartsWith("/uploads/", dto.Url);
			Assert.EndsWith(".jpg", dto.Url, StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			Directory.SetCurrentDirectory(originalDir);
			if (Directory.Exists(tempRoot))
			{
				try { Directory.Delete(tempRoot, true); } catch { /* ignore */ }
			}
		}
	}
}
