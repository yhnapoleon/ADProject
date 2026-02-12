using EcoLens.Api.Data;
using EcoLens.Api.DTOs.UtilityBill;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EcoLens.Tests.Services;

public class UtilityBillServiceTests
{
	private static ApplicationDbContext CreateDb()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase("UtilityBill_" + Guid.NewGuid().ToString("N"))
			.Options;
		return new ApplicationDbContext(options);
	}

	private static IFormFile CreateFakeFile(long length, string fileName = "bill.jpg")
	{
		var mock = new Mock<IFormFile>();
		mock.Setup(f => f.Length).Returns(length);
		mock.Setup(f => f.FileName).Returns(fileName);
		mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[Math.Max(0, (int)length)]));
		return mock.Object;
	}

	[Fact]
	public async Task UploadAndProcessBillAsync_Throws_WhenFileIsNull()
	{
		await using var db = CreateDb();
		var ocr = new Mock<IOcrService>();
		var parser = new Mock<IUtilityBillParser>();
		var calc = new Mock<IUtilityBillCalculationService>();
		var classifier = new Mock<IDocumentTypeClassifier>();
		var sut = new UtilityBillService(db, ocr.Object, parser.Object, calc.Object, classifier.Object, NullLogger<UtilityBillService>.Instance);

		await Assert.ThrowsAsync<ArgumentException>(() => sut.UploadAndProcessBillAsync(1, null!, CancellationToken.None));
	}

	[Fact]
	public async Task UploadAndProcessBillAsync_Throws_WhenFileLengthZero()
	{
		await using var db = CreateDb();
		var ocr = new Mock<IOcrService>();
		var parser = new Mock<IUtilityBillParser>();
		var calc = new Mock<IUtilityBillCalculationService>();
		var classifier = new Mock<IDocumentTypeClassifier>();
		var sut = new UtilityBillService(db, ocr.Object, parser.Object, calc.Object, classifier.Object, NullLogger<UtilityBillService>.Instance);
		var file = CreateFakeFile(0);

		await Assert.ThrowsAsync<ArgumentException>(() => sut.UploadAndProcessBillAsync(1, file, CancellationToken.None));
	}

	[Fact]
	public async Task UploadAndProcessBillAsync_Throws_WhenOcrReturnsEmpty()
	{
		await using var db = CreateDb();
		var ocr = new Mock<IOcrService>();
		ocr.Setup(x => x.RecognizeTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new OcrResult { Text = "", Confidence = 0m });
		var parser = new Mock<IUtilityBillParser>();
		var calc = new Mock<IUtilityBillCalculationService>();
		var classifier = new Mock<IDocumentTypeClassifier>();
		var sut = new UtilityBillService(db, ocr.Object, parser.Object, calc.Object, classifier.Object, NullLogger<UtilityBillService>.Instance);
		var file = CreateFakeFile(100);

		await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UploadAndProcessBillAsync(1, file, CancellationToken.None));
	}

	[Fact]
	public async Task UploadAndProcessBillAsync_Throws_WhenClassificationNotUtilityBill()
	{
		await using var db = CreateDb();
		var ocr = new Mock<IOcrService>();
		ocr.Setup(x => x.RecognizeTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new OcrResult { Text = "some text", Confidence = 0.9m });
		var classifier = new Mock<IDocumentTypeClassifier>();
		classifier.Setup(x => x.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new DocumentTypeClassificationResult { DocumentType = DocumentType.Unknown });
		var parser = new Mock<IUtilityBillParser>();
		var calc = new Mock<IUtilityBillCalculationService>();
		var sut = new UtilityBillService(db, ocr.Object, parser.Object, calc.Object, classifier.Object, NullLogger<UtilityBillService>.Instance);
		var file = CreateFakeFile(100);

		await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UploadAndProcessBillAsync(1, file, CancellationToken.None));
	}

	[Fact]
	public async Task CreateBillManuallyAsync_Throws_WhenStartAfterEnd()
	{
		await using var db = CreateDb();
		var parser = new Mock<IUtilityBillParser>();
		var calc = new Mock<IUtilityBillCalculationService>();
		var classifier = new Mock<IDocumentTypeClassifier>();
		var ocr = new Mock<IOcrService>();
		var sut = new UtilityBillService(db, ocr.Object, parser.Object, calc.Object, classifier.Object, NullLogger<UtilityBillService>.Instance);
		var dto = new CreateUtilityBillManuallyDto
		{
			BillType = UtilityBillType.Electricity,
			BillPeriodStart = new DateTime(2024, 2, 1),
			BillPeriodEnd = new DateTime(2024, 1, 1),
			ElectricityUsage = 100
		};

		await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateBillManuallyAsync(1, dto, CancellationToken.None));
	}

	[Fact]
	public async Task CreateBillManuallyAsync_SavesAndReturns_WhenValid()
	{
		await using var db = CreateDb();
		db.ApplicationUsers.Add(new ApplicationUser { Id = 1, Username = "u", Email = "u@t.com", PasswordHash = "x", Role = UserRole.User, Region = "SG", BirthDate = DateTime.UtcNow.AddYears(-20), IsActive = true });
		await db.SaveChangesAsync();

		var calc = new Mock<IUtilityBillCalculationService>();
		calc.Setup(x => x.CalculateCarbonEmissionAsync(It.IsAny<decimal?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CarbonEmissionResult { ElectricityCarbon = 10, WaterCarbon = 0, GasCarbon = 0, TotalCarbon = 10 });
		var parser = new Mock<IUtilityBillParser>();
		var classifier = new Mock<IDocumentTypeClassifier>();
		var ocr = new Mock<IOcrService>();
		var sut = new UtilityBillService(db, ocr.Object, parser.Object, calc.Object, classifier.Object, NullLogger<UtilityBillService>.Instance);
		var dto = new CreateUtilityBillManuallyDto
		{
			BillType = UtilityBillType.Electricity,
			BillPeriodStart = new DateTime(2024, 1, 1),
			BillPeriodEnd = new DateTime(2024, 1, 31),
			ElectricityUsage = 200
		};

		var result = await sut.CreateBillManuallyAsync(1, dto, CancellationToken.None);

		Assert.True(result.Id > 0);
		Assert.Equal(200, result.ElectricityUsage);
		Assert.Equal(10, result.TotalCarbonEmission);
	}

	[Fact]
	public async Task GetBillByIdAsync_ReturnsNull_WhenNotFound()
	{
		await using var db = CreateDb();
		var ocr = new Mock<IOcrService>();
		var parser = new Mock<IUtilityBillParser>();
		var calc = new Mock<IUtilityBillCalculationService>();
		var classifier = new Mock<IDocumentTypeClassifier>();
		var sut = new UtilityBillService(db, ocr.Object, parser.Object, calc.Object, classifier.Object, NullLogger<UtilityBillService>.Instance);

		var result = await sut.GetBillByIdAsync(999, 1);

		Assert.Null(result);
	}

	[Fact]
	public async Task GetBillByIdAsync_ReturnsDto_WhenFound()
	{
		await using var db = CreateDb();
		db.UtilityBills.Add(new UtilityBill
		{
			UserId = 1,
			BillType = UtilityBillType.Electricity,
			BillPeriodStart = new DateTime(2024, 1, 1),
			BillPeriodEnd = new DateTime(2024, 1, 31),
			YearMonth = "2024-01",
			ElectricityUsage = 100,
			ElectricityCarbonEmission = 5,
			WaterCarbonEmission = 0,
			InputMethod = InputMethod.Manual
		});
		await db.SaveChangesAsync();
		var ocr = new Mock<IOcrService>();
		var parser = new Mock<IUtilityBillParser>();
		var calc = new Mock<IUtilityBillCalculationService>();
		var classifier = new Mock<IDocumentTypeClassifier>();
		var sut = new UtilityBillService(db, ocr.Object, parser.Object, calc.Object, classifier.Object, NullLogger<UtilityBillService>.Instance);

		var bill = await db.UtilityBills.FirstAsync();
		var result = await sut.GetBillByIdAsync(bill.Id, 1);

		Assert.NotNull(result);
		Assert.Equal(100, result.ElectricityUsage);
	}

	[Fact]
	public async Task DeleteBillAsync_ReturnsFalse_WhenNotFound()
	{
		await using var db = CreateDb();
		var ocr = new Mock<IOcrService>();
		var parser = new Mock<IUtilityBillParser>();
		var calc = new Mock<IUtilityBillCalculationService>();
		var classifier = new Mock<IDocumentTypeClassifier>();
		var sut = new UtilityBillService(db, ocr.Object, parser.Object, calc.Object, classifier.Object, NullLogger<UtilityBillService>.Instance);

		var result = await sut.DeleteBillAsync(999, 1);

		Assert.False(result);
	}

	[Fact]
	public async Task GetUserBillsAsync_AppliesDateFilter()
	{
		await using var db = CreateDb();
		db.UtilityBills.Add(new UtilityBill
		{
			UserId = 1,
			BillType = UtilityBillType.Electricity,
			BillPeriodStart = new DateTime(2024, 1, 1),
			BillPeriodEnd = new DateTime(2024, 1, 31),
			YearMonth = "2024-01",
			ElectricityCarbonEmission = 5,
			WaterCarbonEmission = 0,
			InputMethod = InputMethod.Manual
		});
		await db.SaveChangesAsync();
		var ocr = new Mock<IOcrService>();
		var parser = new Mock<IUtilityBillParser>();
		var calc = new Mock<IUtilityBillCalculationService>();
		var classifier = new Mock<IDocumentTypeClassifier>();
		var sut = new UtilityBillService(db, ocr.Object, parser.Object, calc.Object, classifier.Object, NullLogger<UtilityBillService>.Instance);

		var result = await sut.GetUserBillsAsync(1, new GetUtilityBillsQueryDto { StartDate = new DateTime(2024, 1, 15), Page = 1, PageSize = 10 });

		Assert.NotNull(result);
		Assert.Equal(1, result.TotalCount);
	}

	[Fact]
	public async Task GetUserStatisticsAsync_ReturnsAggregates()
	{
		await using var db = CreateDb();
		db.UtilityBills.Add(new UtilityBill
		{
			UserId = 1,
			BillType = UtilityBillType.Electricity,
			BillPeriodStart = new DateTime(2024, 1, 1),
			BillPeriodEnd = new DateTime(2024, 1, 31),
			YearMonth = "2024-01",
			ElectricityUsage = 100,
			ElectricityCarbonEmission = 10,
			WaterCarbonEmission = 0,
			InputMethod = InputMethod.Manual
		});
		await db.SaveChangesAsync();
		var ocr = new Mock<IOcrService>();
		var parser = new Mock<IUtilityBillParser>();
		var calc = new Mock<IUtilityBillCalculationService>();
		var classifier = new Mock<IDocumentTypeClassifier>();
		var sut = new UtilityBillService(db, ocr.Object, parser.Object, calc.Object, classifier.Object, NullLogger<UtilityBillService>.Instance);

		var result = await sut.GetUserStatisticsAsync(1);

		Assert.Equal(1, result.TotalRecords);
		Assert.Equal(100, result.TotalElectricityUsage);
		Assert.Equal(10, result.TotalCarbonEmission);
		Assert.NotEmpty(result.ByBillType);
	}
}
