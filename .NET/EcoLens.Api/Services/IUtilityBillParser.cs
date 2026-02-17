using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.Services;

/// <summary>Utility bill data extraction service interface.</summary>
public interface IUtilityBillParser
{
	/// <summary>Extract bill data from OCR text.</summary>
	/// <param name="ocrText">OCR text</param>
	/// <param name="expectedType">Expected bill type (optional)</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>Extracted data or null</returns>
	Task<ExtractedBillData?> ParseBillDataAsync(string ocrText, UtilityBillType? expectedType = null, CancellationToken ct = default);
}

/// <summary>Extracted bill data.</summary>
public class ExtractedBillData
{
	/// <summary>Bill type</summary>
	public UtilityBillType BillType { get; set; }

	/// <summary>Electricity usage (kWh)</summary>
	public decimal? ElectricityUsage { get; set; }

	/// <summary>Water usage (m³)</summary>
	public decimal? WaterUsage { get; set; }

	/// <summary>Gas usage (kWh or m³)</summary>
	public decimal? GasUsage { get; set; }

	/// <summary>Bill period start</summary>
	public DateTime? BillPeriodStart { get; set; }

	/// <summary>Bill period end</summary>
	public DateTime? BillPeriodEnd { get; set; }

	/// <summary>Extraction confidence (0-1)</summary>
	public decimal Confidence { get; set; }
}
