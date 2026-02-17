using EcoLens.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EcoLens.Tests.Services;

public class DocumentTypeClassifierTests
{
	private static DocumentTypeClassifier CreateClassifier()
	{
		var logger = new Mock<ILogger<DocumentTypeClassifier>>();
		return new DocumentTypeClassifier(logger.Object);
	}

	[Fact]
	public async Task ClassifyAsync_ReturnsUnknown_WhenOcrTextIsNull()
	{
		var classifier = CreateClassifier();
		var result = await classifier.ClassifyAsync(null!);
		Assert.Equal(DocumentType.Unknown, result.DocumentType);
		Assert.Equal(0, result.ConfidenceScore);
		Assert.Contains("OCR text is empty", result.ErrorMessage);
	}

	[Fact]
	public async Task ClassifyAsync_ReturnsUnknown_WhenOcrTextIsEmptyOrWhitespace()
	{
		var classifier = CreateClassifier();
		var result = await classifier.ClassifyAsync("");
		Assert.Equal(DocumentType.Unknown, result.DocumentType);
		Assert.Contains("OCR text is empty", result.ErrorMessage);

		result = await classifier.ClassifyAsync("   \t\n");
		Assert.Equal(DocumentType.Unknown, result.DocumentType);
	}

	[Fact]
	public async Task ClassifyAsync_ReturnsUtilityBill_WhenEnoughUtilityKeywordsAndPatterns()
	{
		var classifier = CreateClassifier();
		// 关键词: electricity, consumption, kwh, bill -> 至少 3 分；或加 pattern 如 "100 kwh"
		var ocrText = "SP Group electricity bill. Consumption 100 kwh. Billing period from Jan to Feb. Account number: 12345.";
		var result = await classifier.ClassifyAsync(ocrText);
		Assert.Equal(DocumentType.UtilityBill, result.DocumentType);
		Assert.True(result.IsUtilityBill);
		Assert.True(result.ConfidenceScore >= 30); // utilityScore >= 3 -> * 10
		Assert.Null(result.ErrorMessage);
		Assert.NotNull(result.MatchedKeywords);
		Assert.True(result.MatchedKeywords.Count > 0);
	}

	[Fact]
	public async Task ClassifyAsync_ReturnsFlightTicket_WhenFlightKeywordsPresent()
	{
		var classifier = CreateClassifier();
		var ocrText = "Singapore Airlines flight SQ123. Boarding pass. Departure gate 12.";
		var result = await classifier.ClassifyAsync(ocrText);
		Assert.Equal(DocumentType.FlightTicket, result.DocumentType);
		Assert.False(result.IsUtilityBill);
		Assert.NotNull(result.ErrorMessage);
		Assert.Contains("flight ticket", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ClassifyAsync_ReturnsReceipt_WhenReceiptKeywordsPresent()
	{
		var classifier = CreateClassifier();
		var ocrText = "Thank you for your purchase. Receipt. Subtotal 10.00. Total amount paid. Card payment.";
		var result = await classifier.ClassifyAsync(ocrText);
		Assert.Equal(DocumentType.Receipt, result.DocumentType);
		Assert.False(result.IsUtilityBill);
		Assert.NotNull(result.ErrorMessage);
		Assert.Contains("receipt", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ClassifyAsync_ReturnsInvoice_WhenInvoiceKeywordsPresent()
	{
		var classifier = CreateClassifier();
		var ocrText = "Tax invoice. Invoice number INV-001. Company ABC. GST registration.";
		var result = await classifier.ClassifyAsync(ocrText);
		Assert.Equal(DocumentType.Invoice, result.DocumentType);
		Assert.False(result.IsUtilityBill);
		Assert.NotNull(result.ErrorMessage);
		Assert.Contains("invoice", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ClassifyAsync_ReturnsUnknown_WhenNoEnoughMatches()
	{
		var classifier = CreateClassifier();
		var ocrText = "Random text with no meaningful keywords.";
		var result = await classifier.ClassifyAsync(ocrText);
		Assert.Equal(DocumentType.Unknown, result.DocumentType);
		Assert.False(result.IsUtilityBill);
		Assert.NotNull(result.ErrorMessage);
		Assert.Contains("Unable to identify", result.ErrorMessage);
	}

	[Fact]
	public async Task ClassifyAsync_UtilityBillWins_WhenMultipleTypesMatch()
	{
		var classifier = CreateClassifier();
		// 水电账单关键词足够多时，应判为 UtilityBill 而非其他
		var ocrText = "SP Group electricity bill. Billing period. Account number 123. Water consumption. Total amount due.";
		var result = await classifier.ClassifyAsync(ocrText);
		Assert.Equal(DocumentType.UtilityBill, result.DocumentType);
	}
}
