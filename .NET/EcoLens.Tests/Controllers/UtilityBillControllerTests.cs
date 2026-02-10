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
            => Task.FromResult(new PagedResultDto<UtilityBillResponseDto>());

        public Task<UtilityBillResponseDto?> GetBillByIdAsync(int id, int userId, CancellationToken ct = default)
            => Task.FromResult<UtilityBillResponseDto?>(null);

        public Task<bool> DeleteBillAsync(int id, int userId, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<UtilityBillStatisticsDto> GetUserStatisticsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
            => Task.FromResult(new UtilityBillStatisticsDto());
    }

    private static UtilityBillController CreateController(int? userId, FakeUtilityBillService service)
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
}

