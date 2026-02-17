namespace EcoLens.Api.Services;

/// <summary>Utility bill carbon emission calculation service interface.</summary>
public interface IUtilityBillCalculationService
{
	/// <summary>Calculate carbon emission from usage.</summary>
	/// <param name="electricityUsage">Electricity (kWh), optional</param>
	/// <param name="waterUsage">Water (m³), optional</param>
	/// <param name="gasUsage">Gas (kWh or m³), optional</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>Carbon emission result</returns>
	Task<CarbonEmissionResult> CalculateCarbonEmissionAsync(
		decimal? electricityUsage,
		decimal? waterUsage,
		decimal? gasUsage,
		CancellationToken ct = default);
}

/// <summary>Carbon emission calculation result.</summary>
public class CarbonEmissionResult
{
	/// <summary>Electricity carbon (kg CO2)</summary>
	public decimal ElectricityCarbon { get; set; }

	/// <summary>Water carbon (kg CO2)</summary>
	public decimal WaterCarbon { get; set; }

	/// <summary>Gas carbon (kg CO2)</summary>
	public decimal GasCarbon { get; set; }

	/// <summary>Total carbon (kg CO2)</summary>
	public decimal TotalCarbon { get; set; }
}
