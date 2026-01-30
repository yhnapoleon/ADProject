using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.DTOs.UtilityBill;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Services;

/// <summary>
/// 水电账单业务服务实现
/// </summary>
public class UtilityBillService : IUtilityBillService
{
	private readonly ApplicationDbContext _db;
	private readonly IOcrService _ocrService;
	private readonly IUtilityBillParser _parser;
	private readonly IUtilityBillCalculationService _calculationService;
	private readonly ILogger<UtilityBillService> _logger;

	public UtilityBillService(
		ApplicationDbContext db,
		IOcrService ocrService,
		IUtilityBillParser parser,
		IUtilityBillCalculationService calculationService,
		ILogger<UtilityBillService> logger)
	{
		_db = db;
		_ocrService = ocrService;
		_parser = parser;
		_calculationService = calculationService;
		_logger = logger;
	}

	/// <summary>
	/// 上传账单文件并识别（不保存到数据库，仅返回识别结果供用户确认）
	/// </summary>
	public async Task<UtilityBillResponseDto> UploadAndProcessBillAsync(int userId, IFormFile file, CancellationToken ct = default)
	{
		try
		{
			// 1. 验证文件
			ValidateFile(file);

			// 2. OCR识别
			OcrResult? ocrResult;
			using (var stream = file.OpenReadStream())
			{
				ocrResult = await _ocrService.RecognizeTextAsync(stream, ct);
			}

			if (ocrResult == null || string.IsNullOrWhiteSpace(ocrResult.Text))
			{
				_logger.LogWarning("OCR recognition failed or returned empty text for user {UserId}", userId);
				throw new InvalidOperationException("无法识别账单，请确保图片清晰或使用手动输入");
			}

			_logger.LogInformation("OCR completed: {TextLength} characters, Confidence: {Confidence}", ocrResult.Text.Length, ocrResult.Confidence);

			// 3. 数据提取
			var extractedData = await _parser.ParseBillDataAsync(ocrResult.Text, null, ct);
			if (extractedData == null)
			{
				_logger.LogWarning("Failed to extract bill data from OCR text for user {UserId}", userId);
				throw new InvalidOperationException("无法从账单中提取数据，请使用手动输入");
			}

			// 4. 验证提取的数据
			if (!extractedData.BillPeriodStart.HasValue || !extractedData.BillPeriodEnd.HasValue)
			{
				_logger.LogWarning("Missing bill period dates in extracted data for user {UserId}", userId);
				throw new InvalidOperationException("无法提取账单周期，请使用手动输入");
			}
			
			// 验证日期范围（确保年份在2000-2100范围内）
			var startYear = extractedData.BillPeriodStart.Value.Year;
			var endYear = extractedData.BillPeriodEnd.Value.Year;
			
			_logger.LogInformation("Validating extracted dates: Start={StartDate} (Year: {StartYear}), End={EndDate} (Year: {EndYear})", 
				extractedData.BillPeriodStart.Value, startYear, extractedData.BillPeriodEnd.Value, endYear);
			
			if (startYear < 2000 || startYear > 2100 || endYear < 2000 || endYear > 2100)
			{
				_logger.LogError("CRITICAL: Invalid date range detected! Start={StartDate} (Year: {StartYear}), End={EndDate} (Year: {EndYear})", 
					extractedData.BillPeriodStart.Value, startYear, extractedData.BillPeriodEnd.Value, endYear);
				throw new InvalidOperationException($"提取的账单周期日期无效（开始年份: {startYear}, 结束年份: {endYear}），请使用手动输入");
			}

			// 5. 计算碳排放
			var carbonResult = await _calculationService.CalculateCarbonEmissionAsync(
				extractedData.ElectricityUsage,
				extractedData.WaterUsage,
				extractedData.GasUsage,
				ct);

			// 6. 创建临时实体（不保存到数据库）
			var utilityBill = new UtilityBill
			{
				Id = 0, // 表示还未保存
				UserId = userId,
				BillType = extractedData.BillType,
				BillPeriodStart = extractedData.BillPeriodStart.Value,
				BillPeriodEnd = extractedData.BillPeriodEnd.Value,
				ElectricityUsage = extractedData.ElectricityUsage,
				WaterUsage = extractedData.WaterUsage,
				GasUsage = extractedData.GasUsage,
				ElectricityCarbonEmission = carbonResult.ElectricityCarbon,
				WaterCarbonEmission = carbonResult.WaterCarbon,
				GasCarbonEmission = carbonResult.GasCarbon,
				TotalCarbonEmission = carbonResult.TotalCarbon,
				OcrRawText = ocrResult.Text,
				OcrConfidence = ocrResult.Confidence,
				InputMethod = InputMethod.Auto,
				CreatedAt = DateTime.UtcNow
			};

			_logger.LogInformation("Bill recognized successfully (not saved): UserId={UserId}, Period={Start} to {End}, Type={Type}",
				userId, extractedData.BillPeriodStart.Value, extractedData.BillPeriodEnd.Value, extractedData.BillType);

			// 7. 转换为DTO返回（Id = 0 表示还未保存，用户确认后需调用 manual 接口保存）
			return ToResponseDto(utilityBill);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error uploading and processing bill for user {UserId}", userId);
			throw;
		}
	}

	/// <summary>
	/// 手动创建账单
	/// </summary>
	public async Task<UtilityBillResponseDto> CreateBillManuallyAsync(int userId, CreateUtilityBillManuallyDto dto, CancellationToken ct = default)
	{
		try
		{
			// 1. 验证输入数据
			if (dto.BillPeriodStart >= dto.BillPeriodEnd)
			{
				throw new ArgumentException("账单周期开始日期必须早于结束日期");
			}

			// 2. 检查重复账单（同一用户、相同账单周期、相同类型、相同用量）
			// 用日期范围比较避免时区/时间部分导致漏判
			var startDay = dto.BillPeriodStart.Date;
			var startNext = startDay.AddDays(1);
			var endDay = dto.BillPeriodEnd.Date;
			var endNext = endDay.AddDays(1);
			var existingBill = await _db.UtilityBills
				.Where(b => b.UserId == userId &&
				            b.BillType == dto.BillType &&
				            b.BillPeriodStart >= startDay && b.BillPeriodStart < startNext &&
				            b.BillPeriodEnd >= endDay && b.BillPeriodEnd < endNext &&
				            ((b.ElectricityUsage.HasValue && dto.ElectricityUsage.HasValue && b.ElectricityUsage.Value == dto.ElectricityUsage.Value) ||
				             (!b.ElectricityUsage.HasValue && !dto.ElectricityUsage.HasValue)) &&
				            ((b.WaterUsage.HasValue && dto.WaterUsage.HasValue && b.WaterUsage.Value == dto.WaterUsage.Value) ||
				             (!b.WaterUsage.HasValue && !dto.WaterUsage.HasValue)) &&
				            ((b.GasUsage.HasValue && dto.GasUsage.HasValue && b.GasUsage.Value == dto.GasUsage.Value) ||
				             (!b.GasUsage.HasValue && !dto.GasUsage.HasValue)))
				.FirstOrDefaultAsync(ct);

			if (existingBill != null)
			{
				_logger.LogWarning("Duplicate bill detected for user {UserId}: BillId={BillId}, Period={Start} to {End}, Type={Type}",
					userId, existingBill.Id, dto.BillPeriodStart, dto.BillPeriodEnd, dto.BillType);
				throw new InvalidOperationException($"检测到重复账单。该账单已存在（账单ID: {existingBill.Id}，账单周期: {dto.BillPeriodStart:yyyy-MM-dd} 至 {dto.BillPeriodEnd:yyyy-MM-dd}）。请勿重复输入相同账单。");
			}

			// 3. 计算碳排放
			var carbonResult = await _calculationService.CalculateCarbonEmissionAsync(
				dto.ElectricityUsage,
				dto.WaterUsage,
				dto.GasUsage,
				ct);

			// 4. 创建实体
			var periodLabel = dto.BillPeriodStart.ToString("yyyy-MM");
			var utilityBill = new UtilityBill
			{
				UserId = userId,
				YearMonth = periodLabel,
				BillType = dto.BillType,
				BillPeriodStart = dto.BillPeriodStart,
				BillPeriodEnd = dto.BillPeriodEnd,
				ElectricityUsage = dto.ElectricityUsage,
				WaterUsage = dto.WaterUsage,
				GasUsage = dto.GasUsage,
				ElectricityCarbonEmission = carbonResult.ElectricityCarbon,
				WaterCarbonEmission = carbonResult.WaterCarbon,
				GasCarbonEmission = carbonResult.GasCarbon,
				TotalCarbonEmission = carbonResult.TotalCarbon,
				InputMethod = InputMethod.Manual
			};

			// 5. 保存到数据库
			await _db.UtilityBills.AddAsync(utilityBill, ct);
			await _db.SaveChangesAsync(ct);

			_logger.LogInformation("Utility bill created manually: UserId={UserId}, BillId={BillId}", userId, utilityBill.Id);

			// 6. 生成 ActivityLog，使仪表盘与 Records 能显示水电账单数据
			try
			{
				// 只按 LabelName + Category 查，不依赖 Region，确保能命中种子数据（Electricity/Water/Gas, Utility）
				async Task<CarbonReference?> FindUtilityFactorAsync(string label)
				{
					return await _db.CarbonReferences
						.AsNoTracking()
						.FirstOrDefaultAsync(
							c => c.LabelName == label && c.Category == CarbonCategory.Utility,
							ct);
				}

				var now = DateTime.UtcNow;
				async Task AddActivityLogAsync(string label, decimal usage, decimal emission)
				{
					if (usage <= 0 && emission <= 0) return;
					var factor = await FindUtilityFactorAsync(label);
					if (factor is null)
					{
						_logger.LogWarning("CarbonReference not found for Utility label={Label}, skip ActivityLog", label);
						return;
					}

					var log = new ActivityLog
					{
						UserId = userId,
						CarbonReferenceId = factor.Id,
						Quantity = usage,
						TotalEmission = emission,
						DetectedLabel = $"{label} ({periodLabel})",
						CreatedAt = now,
						UpdatedAt = now
					};
					_db.ActivityLogs.Add(log);
				}

				await AddActivityLogAsync("Electricity", utilityBill.ElectricityUsage ?? 0m, utilityBill.ElectricityCarbonEmission);
				await AddActivityLogAsync("Water", utilityBill.WaterUsage ?? 0m, utilityBill.WaterCarbonEmission);
				await AddActivityLogAsync("Gas", utilityBill.GasUsage ?? 0m, utilityBill.GasCarbonEmission);

				await _db.SaveChangesAsync(ct);
			}
			catch (Exception exLog)
			{
				// 账单已保存，ActivityLog 失败时仅记录日志、不抛错，用户仍得到 200 与保存成功
				_logger.LogError(exLog, "Failed to create ActivityLogs for UtilityBill UserId={UserId} BillId={BillId}", userId, utilityBill.Id);
			}

			// 7. 转换为DTO返回
			return ToResponseDto(utilityBill);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating bill manually for user {UserId}", userId);
			throw;
		}
	}

	/// <summary>
	/// 获取用户的账单列表
	/// </summary>
	public async Task<PagedResultDto<UtilityBillResponseDto>> GetUserBillsAsync(
		int userId,
		GetUtilityBillsQueryDto? query = null,
		CancellationToken ct = default)
	{
		query ??= new GetUtilityBillsQueryDto();

		var baseQuery = _db.UtilityBills.Where(b => b.UserId == userId);

		// 日期范围筛选（基于账单周期）
		if (query.StartDate.HasValue)
		{
			baseQuery = baseQuery.Where(b => b.BillPeriodEnd >= query.StartDate.Value);
		}

		if (query.EndDate.HasValue)
		{
			var endDateInclusive = query.EndDate.Value.Date.AddDays(1);
			baseQuery = baseQuery.Where(b => b.BillPeriodStart < endDateInclusive);
		}

		// 账单类型筛选
		if (query.BillType.HasValue)
		{
			baseQuery = baseQuery.Where(b => b.BillType == query.BillType.Value);
		}

		// 获取总记录数
		var totalCount = await baseQuery.CountAsync(ct);

		// 排序和分页
		var bills = await baseQuery
			.OrderByDescending(b => b.CreatedAt)
			.Skip((query.Page - 1) * query.PageSize)
			.Take(query.PageSize)
			.ToListAsync(ct);

		// 转换为DTO
		var items = bills.Select(ToResponseDto).ToList();

		return new PagedResultDto<UtilityBillResponseDto>
		{
			Items = items,
			TotalCount = totalCount,
			Page = query.Page,
			PageSize = query.PageSize
		};
	}

	/// <summary>
	/// 根据ID获取单个账单详情
	/// </summary>
	public async Task<UtilityBillResponseDto?> GetBillByIdAsync(int id, int userId, CancellationToken ct = default)
	{
		var bill = await _db.UtilityBills
			.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, ct);

		if (bill == null)
		{
			return null;
		}

		return ToResponseDto(bill);
	}

	/// <summary>
	/// 删除账单
	/// </summary>
	public async Task<bool> DeleteBillAsync(int id, int userId, CancellationToken ct = default)
	{
		var bill = await _db.UtilityBills
			.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, ct);

		if (bill == null)
		{
			return false;
		}

		_db.UtilityBills.Remove(bill);
		await _db.SaveChangesAsync(ct);

		_logger.LogInformation("Utility bill deleted: UserId={UserId}, BillId={BillId}", userId, id);

		return true;
	}

	/// <summary>
	/// 获取用户的账单统计信息
	/// </summary>
	public async Task<UtilityBillStatisticsDto> GetUserStatisticsAsync(
		int userId,
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken ct = default)
	{
		var baseQuery = _db.UtilityBills.Where(b => b.UserId == userId);

		// 日期范围筛选
		if (startDate.HasValue)
		{
			baseQuery = baseQuery.Where(b => b.BillPeriodEnd >= startDate.Value);
		}

		if (endDate.HasValue)
		{
			var endDateInclusive = endDate.Value.Date.AddDays(1);
			baseQuery = baseQuery.Where(b => b.BillPeriodStart < endDateInclusive);
		}

		// 总体统计
		var totalRecords = await baseQuery.CountAsync(ct);
		var totalElectricityUsage = await baseQuery.SumAsync(b => b.ElectricityUsage ?? 0, ct);
		var totalWaterUsage = await baseQuery.SumAsync(b => b.WaterUsage ?? 0, ct);
		var totalGasUsage = await baseQuery.SumAsync(b => b.GasUsage ?? 0, ct);
		var totalCarbonEmission = await baseQuery.SumAsync(b => b.TotalCarbonEmission, ct);

		// 按账单类型分组统计
		var byBillType = await baseQuery
			.GroupBy(b => b.BillType)
			.Select(g => new
			{
				BillType = g.Key,
				RecordCount = g.Count(),
				TotalElectricityUsage = g.Sum(b => b.ElectricityUsage ?? 0),
				TotalWaterUsage = g.Sum(b => b.WaterUsage ?? 0),
				TotalGasUsage = g.Sum(b => b.GasUsage ?? 0),
				TotalCarbonEmission = g.Sum(b => b.TotalCarbonEmission)
			})
			.ToListAsync(ct);

		var byBillTypeList = byBillType.Select(g => new BillTypeStatisticsDto
		{
			BillType = g.BillType,
			BillTypeName = GetBillTypeName(g.BillType),
			RecordCount = g.RecordCount,
			TotalElectricityUsage = g.TotalElectricityUsage,
			TotalWaterUsage = g.TotalWaterUsage,
			TotalGasUsage = g.TotalGasUsage,
			TotalCarbonEmission = g.TotalCarbonEmission
		}).ToList();

		return new UtilityBillStatisticsDto
		{
			TotalRecords = totalRecords,
			TotalElectricityUsage = totalElectricityUsage,
			TotalWaterUsage = totalWaterUsage,
			TotalGasUsage = totalGasUsage,
			TotalCarbonEmission = totalCarbonEmission,
			ByBillType = byBillTypeList
		};
	}

	/// <summary>
	/// 验证上传文件
	/// </summary>
	private void ValidateFile(IFormFile file)
	{
		if (file == null || file.Length == 0)
		{
			throw new ArgumentException("文件不能为空");
		}

		// 文件大小限制（10MB）
		if (file.Length > 10 * 1024 * 1024)
		{
			throw new ArgumentException("文件大小不能超过10MB");
		}

		// 文件类型验证
		// 注意：虽然允许上传PDF，但Google Vision API不支持直接处理PDF
		// 建议用户将PDF转换为图片（JPG/PNG）后再上传
		var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".pdf" };
		var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
		if (!allowedExtensions.Contains(extension))
		{
			throw new ArgumentException($"不支持的文件类型。支持的类型：{string.Join(", ", allowedExtensions)}");
		}
	}

	/// <summary>
	/// 将实体转换为响应DTO
	/// </summary>
	private UtilityBillResponseDto ToResponseDto(UtilityBill bill)
	{
		return new UtilityBillResponseDto
		{
			Id = bill.Id,
			BillType = bill.BillType,
			BillTypeName = GetBillTypeName(bill.BillType),
			BillPeriodStart = bill.BillPeriodStart,
			BillPeriodEnd = bill.BillPeriodEnd,
			ElectricityUsage = bill.ElectricityUsage,
			WaterUsage = bill.WaterUsage,
			GasUsage = bill.GasUsage,
			ElectricityCarbonEmission = bill.ElectricityCarbonEmission,
			WaterCarbonEmission = bill.WaterCarbonEmission,
			GasCarbonEmission = bill.GasCarbonEmission,
			TotalCarbonEmission = bill.TotalCarbonEmission,
			InputMethod = bill.InputMethod,
			InputMethodName = GetInputMethodName(bill.InputMethod),
			OcrConfidence = bill.OcrConfidence,
			OcrRawText = bill.OcrRawText, // Include OCR text for debugging
			CreatedAt = bill.CreatedAt
		};
	}

	/// <summary>
	/// 获取账单类型中文名称
	/// </summary>
	private string GetBillTypeName(UtilityBillType billType)
	{
		return billType switch
		{
			UtilityBillType.Electricity => "电费",
			UtilityBillType.Water => "水费",
			UtilityBillType.Gas => "燃气费",
			UtilityBillType.Combined => "综合账单",
			_ => billType.ToString()
		};
	}

	/// <summary>
	/// 获取输入方式中文名称
	/// </summary>
	private string GetInputMethodName(InputMethod inputMethod)
	{
		return inputMethod switch
		{
			InputMethod.Auto => "自动识别",
			InputMethod.Manual => "手动输入",
			_ => inputMethod.ToString()
		};
	}
}
