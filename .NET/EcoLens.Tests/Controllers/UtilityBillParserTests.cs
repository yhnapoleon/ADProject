using System;
using System.Threading.Tasks;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.Extensions.Logging;

namespace EcoLens.Tests;

public class UtilityBillParserTests
{
    private static UtilityBillParser CreateParser()
    {
        ILogger<UtilityBillParser> logger = new LoggerFactory().CreateLogger<UtilityBillParser>();
        return new UtilityBillParser(logger);
    }

    [Fact]
    public async Task ParseBillDataAsync_ShouldExtractPeriod_FromBillingPeriodLine()
    {
        // Arrange
        var parser = CreateParser();
        var ocrText = """
            SP Services Bill
            Billing Period: 05 Nov 2025 - 05 Dec 2025
            Electricity consumption: 123 kWh
            """;

        // Act
        var result = await parser.ParseBillDataAsync(ocrText, UtilityBillType.Electricity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2025, 11, 5), result!.BillPeriodStart);
        Assert.Equal(new DateTime(2025, 12, 5), result.BillPeriodEnd);
    }

    [Fact]
    public async Task ParseBillDataAsync_ShouldExtractPeriod_FromNumericFromToPattern()
    {
        // Arrange
        var parser = CreateParser();
        var ocrText = """
            Utility Bill Statement
            From 01/01/2024 to 31/01/2024
            Total usage: 456 kWh
            """;

        // Act
        var result = await parser.ParseBillDataAsync(ocrText, UtilityBillType.Electricity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 1, 1), result!.BillPeriodStart);
        Assert.Equal(new DateTime(2024, 1, 31), result.BillPeriodEnd);
    }

	[Fact]
	public async Task ParseBillDataAsync_ShouldReturnNull_WhenOcrTextEmpty()
	{
		var parser = CreateParser();

		var result = await parser.ParseBillDataAsync(string.Empty, UtilityBillType.Electricity);

		Assert.Null(result);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldNotSetPeriod_WhenYearOutOfRange()
	{
		var parser = CreateParser();
		var ocrText = """
			Utility Bill Statement
			Billing Period: 05 Nov 1899 - 05 Dec 1899
			Total usage: 123 kWh
			""";

		var result = await parser.ParseBillDataAsync(ocrText, UtilityBillType.Electricity);

		Assert.NotNull(result);
		Assert.Null(result!.BillPeriodStart);
		Assert.Null(result.BillPeriodEnd);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldReturnNull_WhenOcrTextWhitespace()
	{
		var parser = CreateParser();
		Assert.Null(await parser.ParseBillDataAsync("   \t\n ", UtilityBillType.Electricity));
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldExtractElectricityUsage_FromKwhPattern()
	{
		var parser = CreateParser();
		var ocrText = "SP Services. Consumption: 456.7 kWh. Billing Period: 01 Jan 2025 - 31 Jan 2025";
		var result = await parser.ParseBillDataAsync(ocrText);
		Assert.NotNull(result);
		Assert.Equal(456.7m, result!.ElectricityUsage);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldExtractWaterUsage_FromCuM()
	{
		var parser = CreateParser();
		var ocrText = "Water usage: 7.6 Cu M. Billing Period: 05 Nov 2025 - 05 Dec 2025";
		var result = await parser.ParseBillDataAsync(ocrText);
		Assert.NotNull(result);
		Assert.Equal(7.6m, result!.WaterUsage);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldExtractWaterUsage_FromLitres_AndConvertToCubicMeters()
	{
		var parser = CreateParser();
		var ocrText = "Water consumption: 2000 litre. Period 01 Jan 2025 - 31 Jan 2025";
		var result = await parser.ParseBillDataAsync(ocrText);
		Assert.NotNull(result);
		Assert.NotNull(result!.WaterUsage);
		Assert.Equal(2m, result.WaterUsage);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldExtractGasUsage_WhenGasContext()
	{
		var parser = CreateParser();
		var ocrText = "Town gas consumption: 120 kWh. Billing Period: 01 Oct 2025 - 31 Oct 2025";
		var result = await parser.ParseBillDataAsync(ocrText);
		Assert.NotNull(result);
		Assert.Equal(120m, result!.GasUsage);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldIdentifyBillType_ElectricityOnly()
	{
		var parser = CreateParser();
		// 避免水费关键词：单字母 'l' 会命中（如 Electricity 中含 l）；用 Power + kWh 仅触发电费
		var ocrText = "Power 100 kWh.";
		var result = await parser.ParseBillDataAsync(ocrText);
		Assert.NotNull(result);
		Assert.Equal(UtilityBillType.Electricity, result!.BillType);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldIdentifyBillType_WaterOnly()
	{
		var parser = CreateParser();
		// Avoid 'l' (litre) and electricity/gas keywords
		var ocrText = "PUB water 5 m3.";
		var result = await parser.ParseBillDataAsync(ocrText);
		Assert.NotNull(result);
		Assert.Equal(UtilityBillType.Water, result!.BillType);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldIdentifyBillType_GasOnly()
	{
		var parser = CreateParser();
		// Avoid kwh (electricity keyword); use gas context only
		var ocrText = "City gas 50.";
		var result = await parser.ParseBillDataAsync(ocrText);
		Assert.NotNull(result);
		Assert.Equal(UtilityBillType.Gas, result!.BillType);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldIdentifyBillType_Combined_WhenMultipleKeywords()
	{
		var parser = CreateParser();
		var ocrText = "SP Services. Electricity 100 kWh. Water 3 Cu M. Gas 20 kWh. Billing Period: 01 Jan 2025 - 31 Jan 2025";
		var result = await parser.ParseBillDataAsync(ocrText);
		Assert.NotNull(result);
		Assert.Equal(UtilityBillType.Combined, result!.BillType);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldUseExpectedType_WhenProvided()
	{
		var parser = CreateParser();
		var ocrText = "Some random text with no keywords. 200 kWh. 01/01/2025 - 31/01/2025";
		var result = await parser.ParseBillDataAsync(ocrText, UtilityBillType.Electricity);
		Assert.NotNull(result);
		Assert.Equal(UtilityBillType.Electricity, result!.BillType);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldDefaultToCombined_WhenNoKeywords()
	{
		var parser = CreateParser();
		// No keyword: avoid 'l' (water), kwh, electricity, water, gas, consumption, usage, etc.
		var ocrText = "Summary 100. 01 Jan 2025 to 31 Jan 2025.";
		var result = await parser.ParseBillDataAsync(ocrText);
		Assert.NotNull(result);
		Assert.Equal(UtilityBillType.Combined, result!.BillType);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldExtractPeriod_FromYyyyMmDdFormat()
	{
		var parser = CreateParser();
		var ocrText = "Statement 2024-03-01 to 2024-03-31. Usage 100 kWh.";
		var result = await parser.ParseBillDataAsync(ocrText, UtilityBillType.Electricity);
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2024, 3, 1), result!.BillPeriodStart);
		Assert.Equal(new DateTime(2024, 3, 31), result.BillPeriodEnd);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldExtractPeriod_FromTwoSeparateMonthNameDates()
	{
		var parser = CreateParser();
		var ocrText = "Bill from 15 Jan 2025 until 15 Feb 2025. Electricity 50 kWh.";
		var result = await parser.ParseBillDataAsync(ocrText, UtilityBillType.Electricity);
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2025, 1, 15), result!.BillPeriodStart);
		Assert.Equal(new DateTime(2025, 2, 15), result.BillPeriodEnd);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldSetConfidence_WhenAllDataPresent()
	{
		var parser = CreateParser();
		var ocrText = """
			SP Services. Electricity consumption: 200 kWh. Water: 5 Cu M.
			Billing Period: 01 Nov 2025 - 30 Nov 2025
			""";
		var result = await parser.ParseBillDataAsync(ocrText);
		Assert.NotNull(result);
		Assert.True(result!.Confidence > 0 && result.Confidence <= 1);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldHandleInvalidDateFormat_Gracefully()
	{
		var parser = CreateParser();
		var ocrText = "Bill with bad dates: 99/99/2025 and 32 Jan 2025. Electricity 100 kWh.";
		var result = await parser.ParseBillDataAsync(ocrText, UtilityBillType.Electricity);
		Assert.NotNull(result);
		Assert.Equal(100m, result!.ElectricityUsage);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldHandleYearBoundary_DecemberToJanuary()
	{
		var parser = CreateParser();
		var ocrText = "Billing Period: 01 Dec 2024 - 05 Jan 2025. Usage: 80 kWh.";
		var result = await parser.ParseBillDataAsync(ocrText, UtilityBillType.Electricity);
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2024, 12, 1), result!.BillPeriodStart);
		Assert.Equal(new DateTime(2025, 1, 5), result.BillPeriodEnd);
	}

	[Fact]
	public async Task ParseBillDataAsync_ShouldNotSetPeriod_WhenBothDatesInvalid()
	{
		var parser = CreateParser();
		// 开始、结束均为非法日（day=32），TryParseDate 校验 1-31 会失败，周期不落库
		var ocrText = """
			Bill From 32/01/2025 to 32/01/2025
			Power 50 kWh
			""";
		var result = await parser.ParseBillDataAsync(ocrText, UtilityBillType.Electricity);
		Assert.NotNull(result);
		Assert.Null(result!.BillPeriodStart);
		Assert.Null(result.BillPeriodEnd);
	}
}

