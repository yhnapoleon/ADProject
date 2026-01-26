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
	/// 上传账单文件并自动处理
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

			// 5. 检查重复账单（同一用户、相同账单周期、相同类型、相同用量）
			// 注意：decimal? 类型需要特殊处理，使用 HasValue 和 Value 比较
			var existingBill = await _db.UtilityBills
				.Where(b => b.UserId == userId &&
				            b.BillType == extractedData.BillType &&
				            b.BillPeriodStart == extractedData.BillPeriodStart.Value &&
				            b.BillPeriodEnd == extractedData.BillPeriodEnd.Value &&
				            ((b.ElectricityUsage.HasValue && extractedData.ElectricityUsage.HasValue && b.ElectricityUsage.Value == extractedData.ElectricityUsage.Value) ||
				             (!b.ElectricityUsage.HasValue && !extractedData.ElectricityUsage.HasValue)) &&
				            ((b.WaterUsage.HasValue && extractedData.WaterUsage.HasValue && b.WaterUsage.Value == extractedData.WaterUsage.Value) ||
				             (!b.WaterUsage.HasValue && !extractedData.WaterUsage.HasValue)) &&
				            ((b.GasUsage.HasValue && extractedData.GasUsage.HasValue && b.GasUsage.Value == extractedData.GasUsage.Value) ||
				             (!b.GasUsage.HasValue && !extractedData.GasUsage.HasValue)))
				.FirstOrDefaultAsync(ct);

			if (existingBill != null)
			{
				_logger.LogWarning("Duplicate bill detected for user {UserId}: BillId={BillId}, Period={Start} to {End}, Type={Type}",
					userId, existingBill.Id, extractedData.BillPeriodStart.Value, extractedData.BillPeriodEnd.Value, extractedData.BillType);
				throw new InvalidOperationException($"检测到重复账单。该账单已存在（账单ID: {existingBill.Id}，账单周期: {extractedData.BillPeriodStart.Value:yyyy-MM-dd} 至 {extractedData.BillPeriodEnd.Value:yyyy-MM-dd}）。请勿重复上传相同账单。");
			}

			// 6. 计算碳排放
			var carbonResult = await _calculationService.CalculateCarbonEmissionAsync(
				extractedData.ElectricityUsage,
				extractedData.WaterUsage,
				extractedData.GasUsage,
				ct);

			// 7. 创建实体
			var utilityBill = new UtilityBill
			{
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
				InputMethod = InputMethod.Auto
			};

			// 8. 保存到数据库
			await _db.UtilityBills.AddAsync(utilityBill, ct);
			await _db.SaveChangesAsync(ct);

			_logger.LogInformation("Utility bill created successfully: UserId={UserId}, BillId={BillId}", userId, utilityBill.Id);

			// 9. 转换为DTO返回
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
			// 注意：decimal? 类型需要特殊处理，使用 HasValue 和 Value 比较
			var existingBill = await _db.UtilityBills
				.Where(b => b.UserId == userId &&
				            b.BillType == dto.BillType &&
				            b.BillPeriodStart == dto.BillPeriodStart &&
				            b.BillPeriodEnd == dto.BillPeriodEnd &&
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
			var utilityBill = new UtilityBill
			{
				UserId = userId,
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

			// 4. 保存到数据库
			await _db.UtilityBills.AddAsync(utilityBill, ct);
			await _db.SaveChangesAsync(ct);

			_logger.LogInformation("Utility bill created manually: UserId={UserId}, BillId={BillId}", userId, utilityBill.Id);

			// 5. 转换为DTO返回
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
