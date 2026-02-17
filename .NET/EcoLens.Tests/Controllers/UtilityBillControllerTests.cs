using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Controllers;
using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.DTOs.UtilityBill;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EcoLens.Tests;

public class UtilityBillControllerTests
{
    private class FakeFormFile : IFormFile
    {
        private readonly byte[] _content;

        public FakeFormFile(string name, string fileName, string contentType, string content)
        {
            Name = name;
            FileName = fileName;
            ContentType = contentType;
            _content = Encoding.UTF8.GetBytes(content);
        }

        public string ContentType { get; }
        public string ContentDisposition { get; set; } = string.Empty;
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public long Length => _content.Length;
        public string Name { get; }
        public string FileName { get; }

        public void CopyTo(Stream target) => target.Write(_content, 0, _content.Length);

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            target.Write(_content, 0, _content.Length);
            return Task.CompletedTask;
        }

        public Stream OpenReadStream() => new MemoryStream(_content);
    }

    private class FakeUtilityBillService : IUtilityBillService
    {
        public int LastUserId { get; private set; }
        public IFormFile? LastFile { get; private set; }
        public CreateUtilityBillManuallyDto? LastManualDto { get; private set; }

        public int LastGetBillsUserId { get; private set; }
        public GetUtilityBillsQueryDto? LastGetBillsQuery { get; private set; }
        public PagedResultDto<UtilityBillResponseDto>? GetBillsResult { get; set; }

        public int LastGetByIdUserId { get; private set; }
        public int LastGetByIdId { get; private set; }
        public UtilityBillResponseDto? GetByIdResult { get; set; }

        public int LastDeleteUserId { get; private set; }
        public int LastDeleteId { get; private set; }
        public bool DeleteResult { get; set; }

        public Task<UtilityBillResponseDto> UploadAndProcessBillAsync(int userId, IFormFile file, CancellationToken ct = default)
        {
            LastUserId = userId;
            LastFile = file;
            return Task.FromResult(new UtilityBillResponseDto
            {
                Id = 0,
                BillType = UtilityBillType.Electricity,
                BillPeriodStart = new DateTime(2024, 1, 1),
                BillPeriodEnd = new DateTime(2024, 1, 31),
                ElectricityUsage = 123,
                TotalCarbonEmission = 10,
                InputMethod = InputMethod.Auto,
                CreatedAt = DateTime.UtcNow
            });
        }

        public Task<UtilityBillResponseDto> CreateBillManuallyAsync(int userId, CreateUtilityBillManuallyDto dto, CancellationToken ct = default)
        {
            LastUserId = userId;
            LastManualDto = dto;
            return Task.FromResult(new UtilityBillResponseDto
            {
                Id = 1,
                BillType = dto.BillType,
                BillPeriodStart = dto.BillPeriodStart,
                BillPeriodEnd = dto.BillPeriodEnd,
                ElectricityUsage = dto.ElectricityUsage,
                TotalCarbonEmission = 20,
                InputMethod = InputMethod.Manual,
                CreatedAt = DateTime.UtcNow
            });
        }

        public Task<PagedResultDto<UtilityBillResponseDto>> GetUserBillsAsync(int userId, GetUtilityBillsQueryDto? query = null, CancellationToken ct = default)
        {
            LastGetBillsUserId = userId;
            LastGetBillsQuery = query;
            return Task.FromResult(GetBillsResult ?? new PagedResultDto<UtilityBillResponseDto>());
        }

        public Task<UtilityBillResponseDto?> GetBillByIdAsync(int id, int userId, CancellationToken ct = default)
        {
            LastGetByIdId = id;
            LastGetByIdUserId = userId;
            return Task.FromResult<UtilityBillResponseDto?>(GetByIdResult);
        }

        public Task<bool> DeleteBillAsync(int id, int userId, CancellationToken ct = default)
        {
            LastDeleteId = id;
            LastDeleteUserId = userId;
            return Task.FromResult(DeleteResult);
        }

        public Task<UtilityBillStatisticsDto> GetUserStatisticsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
            => Task.FromResult(new UtilityBillStatisticsDto());
    }

    private static UtilityBillController CreateController(int? userId, IUtilityBillService service)
    {
        var logger = new LoggerFactory().CreateLogger<UtilityBillController>();
        var controller = new UtilityBillController(service, logger);

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
    public async Task Upload_ShouldReturnUnauthorized_WhenUserMissing()
    {
        var service = new FakeUtilityBillService();
        var controller = CreateController(null, service);

        var dto = new UploadUtilityBillDto
        {
            File = new FakeFormFile("file", "bill.pdf", "application/pdf", "dummy")
        };

        var result = await controller.Upload(dto, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Contains("Unable to get user information", unauthorized.Value!.ToString());
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenModelStateInvalid()
    {
        var service = new FakeUtilityBillService();
        var controller = CreateController(1, service);
        controller.ModelState.AddModelError("File", "Required");

        var dto = new UploadUtilityBillDto(); // File 未赋值

        var result = await controller.Upload(dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Request validation failed", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task Upload_ShouldCallService_AndReturnOk_OnSuccess()
    {
        var service = new FakeUtilityBillService();
        var controller = CreateController(99, service);

        var dto = new UploadUtilityBillDto
        {
            File = new FakeFormFile("file", "bill.pdf", "application/pdf", "dummy")
        };

        var result = await controller.Upload(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<UtilityBillResponseDto>(ok.Value);

        Assert.Equal(0, body.Id);
        Assert.Equal(UtilityBillType.Electricity, body.BillType);
        Assert.Equal(99, service.LastUserId);
        Assert.NotNull(service.LastFile);
    }

    [Fact]
    public async Task CreateManually_ShouldReturnUnauthorized_WhenUserMissing()
    {
        var service = new FakeUtilityBillService();
        var controller = CreateController(null, service);

        var dto = new CreateUtilityBillManuallyDto
        {
            BillType = UtilityBillType.Electricity,
            BillPeriodStart = new DateTime(2024, 1, 1),
            BillPeriodEnd = new DateTime(2024, 1, 31)
        };

        var result = await controller.CreateManually(dto, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Contains("Unable to get user information", unauthorized.Value!.ToString());
    }

    [Fact]
    public async Task CreateManually_ShouldReturnBadRequest_WhenModelStateInvalid()
    {
        var service = new FakeUtilityBillService();
        var controller = CreateController(1, service);
        controller.ModelState.AddModelError("BillType", "Required");

        var dto = new CreateUtilityBillManuallyDto(); // 缺少必填字段

        var result = await controller.CreateManually(dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Request validation failed", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task CreateManually_ShouldCallService_AndReturnOk_OnSuccess()
    {
        var service = new FakeUtilityBillService();
        var controller = CreateController(77, service);

        var dto = new CreateUtilityBillManuallyDto
        {
            BillType = UtilityBillType.Electricity,
            BillPeriodStart = new DateTime(2024, 1, 1),
            BillPeriodEnd = new DateTime(2024, 1, 31),
            ElectricityUsage = 150.5m
        };

        var result = await controller.CreateManually(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<UtilityBillResponseDto>(ok.Value);

        Assert.Equal(1, body.Id);
        Assert.Equal(UtilityBillType.Electricity, body.BillType);
        Assert.Equal(77, service.LastUserId);
        Assert.Equal(dto.ElectricityUsage, service.LastManualDto!.ElectricityUsage);
    }

    [Fact]
    public async Task CreateManually_ShouldReturnBadRequest_WhenServiceThrowsArgumentException()
    {
        var mock = new Mock<IUtilityBillService>();
        mock.Setup(x => x.CreateBillManuallyAsync(It.IsAny<int>(), It.IsAny<CreateUtilityBillManuallyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid date range"));
        var controller = CreateController(1, mock.Object);
        var dto = new CreateUtilityBillManuallyDto
        {
            BillType = UtilityBillType.Electricity,
            BillPeriodStart = new DateTime(2024, 1, 1),
            BillPeriodEnd = new DateTime(2024, 1, 31)
        };

        var result = await controller.CreateManually(dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid date range", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task CreateManually_ShouldReturnBadRequest_WhenServiceThrowsInvalidOperationException()
    {
        var mock = new Mock<IUtilityBillService>();
        mock.Setup(x => x.CreateBillManuallyAsync(It.IsAny<int>(), It.IsAny<CreateUtilityBillManuallyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Business rule violation"));
        var controller = CreateController(1, mock.Object);
        var dto = new CreateUtilityBillManuallyDto
        {
            BillType = UtilityBillType.Electricity,
            BillPeriodStart = new DateTime(2024, 1, 1),
            BillPeriodEnd = new DateTime(2024, 1, 31)
        };

        var result = await controller.CreateManually(dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Business rule violation", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task CreateManually_ShouldReturnBadRequest_WhenServiceThrowsDuplicateKeyException()
    {
        var mock = new Mock<IUtilityBillService>();
        mock.Setup(x => x.CreateBillManuallyAsync(It.IsAny<int>(), It.IsAny<CreateUtilityBillManuallyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cannot insert duplicate key"));
        var controller = CreateController(1, mock.Object);
        var dto = new CreateUtilityBillManuallyDto
        {
            BillType = UtilityBillType.Electricity,
            BillPeriodStart = new DateTime(2024, 1, 1),
            BillPeriodEnd = new DateTime(2024, 1, 31)
        };

        var result = await controller.CreateManually(dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("duplicate", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateManually_ShouldReturnBadRequest_WhenServiceThrowsFactorNotFound()
    {
        var mock = new Mock<IUtilityBillService>();
        mock.Setup(x => x.CreateBillManuallyAsync(It.IsAny<int>(), It.IsAny<CreateUtilityBillManuallyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Emission factor not found"));
        var controller = CreateController(1, mock.Object);
        var dto = new CreateUtilityBillManuallyDto
        {
            BillType = UtilityBillType.Electricity,
            BillPeriodStart = new DateTime(2024, 1, 1),
            BillPeriodEnd = new DateTime(2024, 1, 31)
        };

        var result = await controller.CreateManually(dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("factor", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateManually_ShouldReturn500_WhenServiceThrowsGenericException()
    {
        var mock = new Mock<IUtilityBillService>();
        mock.Setup(x => x.CreateBillManuallyAsync(It.IsAny<int>(), It.IsAny<CreateUtilityBillManuallyDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidTimeZoneException("Unexpected server error"));
        var controller = CreateController(1, mock.Object);
        var dto = new CreateUtilityBillManuallyDto
        {
            BillType = UtilityBillType.Electricity,
            BillPeriodStart = new DateTime(2024, 1, 1),
            BillPeriodEnd = new DateTime(2024, 1, 31)
        };

        var result = await controller.CreateManually(dto, CancellationToken.None);

        var statusCode = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCode.StatusCode);
        Assert.NotNull(statusCode.Value);
    }

    [Fact]
    public async Task GetMyBills_ShouldReturnUnauthorized_WhenUserMissing()
    {
        var service = new FakeUtilityBillService();
        var controller = CreateController(null, service);

        var query = new GetUtilityBillsQueryDto();

        var result = await controller.GetMyBills(query, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Contains("Unable to get user information", unauthorized.Value!.ToString());
    }

    [Fact]
    public async Task GetMyBills_ShouldCallService_AndReturnOk_OnSuccess()
    {
        var paged = new PagedResultDto<UtilityBillResponseDto>
        {
            Items = new List<UtilityBillResponseDto>
            {
                new()
                {
                    Id = 5,
                    BillType = UtilityBillType.Electricity
                }
            },
            TotalCount = 1,
            Page = 2,
            PageSize = 10
        };

        var service = new FakeUtilityBillService
        {
            GetBillsResult = paged
        };

        var controller = CreateController(10, service);

        var query = new GetUtilityBillsQueryDto
        {
            Page = 2,
            PageSize = 10,
            BillType = UtilityBillType.Electricity
        };

        var result = await controller.GetMyBills(query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<PagedResultDto<UtilityBillResponseDto>>(ok.Value);

        Assert.Equal(10, service.LastGetBillsUserId);
        Assert.Equal(2, service.LastGetBillsQuery!.Page);
        Assert.Single(body.Items);
        Assert.Equal(5, body.Items[0].Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnUnauthorized_WhenUserMissing()
    {
        var service = new FakeUtilityBillService();
        var controller = CreateController(null, service);

        var result = await controller.GetById(123, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Contains("Unable to get user information", unauthorized.Value!.ToString());
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenServiceReturnsNull()
    {
        var service = new FakeUtilityBillService
        {
            GetByIdResult = null
        };
        var controller = CreateController(7, service);

        var result = await controller.GetById(999, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("Bill not found or access denied", notFound.Value!.ToString());
        Assert.Equal(999, service.LastGetByIdId);
        Assert.Equal(7, service.LastGetByIdUserId);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenBillExists()
    {
        var expected = new UtilityBillResponseDto
        {
            Id = 42,
            BillType = UtilityBillType.Water
        };

        var service = new FakeUtilityBillService
        {
            GetByIdResult = expected
        };
        var controller = CreateController(3, service);

        var result = await controller.GetById(42, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<UtilityBillResponseDto>(ok.Value);

        Assert.Equal(42, body.Id);
        Assert.Equal(UtilityBillType.Water, body.BillType);
        Assert.Equal(42, service.LastGetByIdId);
        Assert.Equal(3, service.LastGetByIdUserId);
    }

    [Fact]
    public async Task Delete_ShouldReturnUnauthorized_WhenUserMissing()
    {
        var service = new FakeUtilityBillService();
        var controller = CreateController(null, service);

        var result = await controller.Delete(10, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("Unable to get user information", unauthorized.Value!.ToString());
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenServiceReturnsFalse()
    {
        var service = new FakeUtilityBillService
        {
            DeleteResult = false
        };
        var controller = CreateController(9, service);

        var result = await controller.Delete(88, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Bill not found or access denied", notFound.Value!.ToString());
        Assert.Equal(88, service.LastDeleteId);
        Assert.Equal(9, service.LastDeleteUserId);
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenDeleted()
    {
        var service = new FakeUtilityBillService
        {
            DeleteResult = true
        };
        var controller = CreateController(15, service);

        var result = await controller.Delete(77, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Deleted successfully", ok.Value!.ToString());
        Assert.Equal(77, service.LastDeleteId);
        Assert.Equal(15, service.LastDeleteUserId);
    }
}

