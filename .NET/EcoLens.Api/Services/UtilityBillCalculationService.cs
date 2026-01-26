using EcoLens.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Services;

/// <summary>
/// 水电账单碳排放计算服务实现
/// </summary>
public class UtilityBillCalculationService : IUtilityBillCalculationService
{
	private readonly ApplicationDbContext _db;
	private readonly ILogger<UtilityBillCalculationService> _logger;

	public UtilityBillCalculationService(
		ApplicationDbContext db,
		ILogger<UtilityBillCalculationService> logger)
	{
		_db = db;
		_logger = logger;
	}

	/// <summary>
	/// 根据用量计算碳排放
	/// </summary>
	public async Task<CarbonEmissionResult> CalculateCarbonEmissionAsync(
		decimal? electricityUsage,
		decimal? waterUsage,
		decimal? gasUsage,
		CancellationToken ct = default)
	{
		try
		{
			// 1. 从数据库读取排放因子
			var electricityFactor = await _db.CarbonReferences
				.FirstOrDefaultAsync(c => c.LabelName == "Electricity_SG", ct);

			var waterFactor = await _db.CarbonReferences
				.FirstOrDefaultAsync(c => c.LabelName == "Water_SG", ct);

			var gasFactor = await _db.CarbonReferences
				.FirstOrDefaultAsync(c => c.LabelName == "Gas_SG", ct);

			// 2. 验证排放因子是否存在
			if (electricityFactor == null)
			{
				_logger.LogError("Electricity_SG carbon factor not found in database");
				throw new InvalidOperationException("Electricity carbon emission factor not found");
			}

			if (waterFactor == null)
			{
				_logger.LogError("Water_SG carbon factor not found in database");
				throw new InvalidOperationException("Water carbon emission factor not found");
			}

			if (gasFactor == null)
			{
				_logger.LogError("Gas_SG carbon factor not found in database");
				throw new InvalidOperationException("Gas carbon emission factor not found");
			}

			// 3. 计算各项碳排放
			var electricityCarbon = electricityUsage.HasValue && electricityUsage.Value > 0
				? electricityUsage.Value * electricityFactor.Co2Factor
				: 0m;

			var waterCarbon = waterUsage.HasValue && waterUsage.Value > 0
				? waterUsage.Value * waterFactor.Co2Factor
				: 0m;

			var gasCarbon = gasUsage.HasValue && gasUsage.Value > 0
				? gasUsage.Value * gasFactor.Co2Factor
				: 0m;

			// 4. 计算总碳排放
			var totalCarbon = electricityCarbon + waterCarbon + gasCarbon;

			// 5. 记录日志
			_logger.LogInformation(
				"Carbon emission calculated: Electricity={Electricity} kWh -> {ElectricityCarbon} kg CO2, " +
				"Water={Water} m³ -> {WaterCarbon} kg CO2, " +
				"Gas={Gas} -> {GasCarbon} kg CO2, " +
				"Total={Total} kg CO2",
				electricityUsage, electricityCarbon,
				waterUsage, waterCarbon,
				gasUsage, gasCarbon,
				totalCarbon);

			// 6. 验证计算结果
			if (totalCarbon < 0)
			{
				_logger.LogWarning("Negative carbon emission calculated: {Total}", totalCarbon);
			}

			if (totalCarbon > 1000000) // 1,000,000 kg CO2 = 1000 吨 CO2，异常大
			{
				_logger.LogWarning("Unusually large carbon emission calculated: {Total} kg CO2", totalCarbon);
			}

			return new CarbonEmissionResult
			{
				ElectricityCarbon = electricityCarbon,
				WaterCarbon = waterCarbon,
				GasCarbon = gasCarbon,
				TotalCarbon = totalCarbon
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error calculating carbon emission");
			throw;
		}
	}
}
