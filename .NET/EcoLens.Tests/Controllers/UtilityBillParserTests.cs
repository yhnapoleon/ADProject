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
}

