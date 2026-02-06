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
	private readonly IDocumentTypeClassifier _documentTypeClassifier;
	private readonly ILogger<UtilityBillService> _logger;

	public UtilityBillService(
		ApplicationDbContext db,
		IOcrService ocrService,
		IUtilityBillParser parser,
		IUtilityBillCalculationService calculationService,
		IDocumentTypeClassifier documentTypeClassifier,
		ILogger<UtilityBillService> logger)
	{
		_db = db;
		_ocrService = ocrService;
		_parser = parser;
		_calculationService = calculationService;
		_documentTypeClassifier = documentTypeClassifier;
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
				throw new InvalidOperationException("Unable to recognize bill. Please ensure the image is clear or use manual input.");
			}

			_logger.LogInformation("OCR completed: {TextLength} characters, Confidence: {Confidence}", ocrResult.Text.Length, ocrResult.Confidence);

			// 3. 文档类型验证（必须在水电账单识别之前）
			var classificationResult = await _documentTypeClassifier.ClassifyAsync(ocrResult.Text, ct);
			if (!classificationResult.IsUtilityBill)
			{
				_logger.LogWarning(
					"Document type validation failed for user {UserId}. Detected type: {DocumentType}, Confidence: {Confidence}, MatchedKeywords: {Keywords}",
					userId, classificationResult.DocumentType, classificationResult.ConfidenceScore, 
					string.Join(", ", classificationResult.MatchedKeywords));

				throw new InvalidOperationException(
					classificationResult.ErrorMessage ?? "The uploaded image is not a utility bill. Please upload a Singapore electricity, water or gas bill.");
			}

			_logger.LogInformation(
				"Document type validated as utility bill. Confidence: {Confidence}, MatchedKeywords: {Keywords}",
				classificationResult.ConfidenceScore, string.Join(", ", classificationResult.MatchedKeywords));

			// 4. 数据提取
			var extractedData = await _parser.ParseBillDataAsync(ocrResult.Text, null, ct);
			if (extractedData == null)
			{
				_logger.LogWarning("Failed to extract bill data from OCR text for user {UserId}", userId);
				throw new InvalidOperationException("Unable to extract data from bill. Please use manual input.");
			}

			// 5. 验证提取的数据
			if (!extractedData.BillPeriodStart.HasValue || !extractedData.BillPeriodEnd.HasValue)
			{
				_logger.LogWarning("Missing bill period dates in extracted data for user {UserId}", userId);
				throw new InvalidOperationException("Unable to extract bill period. Please use manual input.");
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
				throw new InvalidOperationException($"Invalid bill period dates (start year: {startYear}, end year: {endYear}). Please use manual input.");
			}

			// 6. 计算碳排放
			var carbonResult = await _calculationService.CalculateCarbonEmissionAsync(
				extractedData.ElectricityUsage,
				extractedData.WaterUsage,
				extractedData.GasUsage,
				ct);

			// 7. 创建临时实体（不保存到数据库）
			// 计算 YearMonth：基于 BillPeriodEnd 的年月，确保月份正确
			var endDate = extractedData.BillPeriodEnd.Value;
			var yearMonth = $"{endDate.Year:D4}-{endDate.Month:D2}"; // 使用 D2 确保月份是两位数（01-12）
			
			_logger.LogInformation("Calculating YearMonth: BillPeriodEnd={EndDate} (Year={Year}, Month={Month}), YearMonth={YearMonth}", 
				endDate, endDate.Year, endDate.Month, yearMonth);
			
			var utilityBill = new UtilityBill
			{
				Id = 0, // 表示还未保存
				UserId = userId,
				BillType = extractedData.BillType,
				BillPeriodStart = extractedData.BillPeriodStart.Value,
				BillPeriodEnd = endDate,
				YearMonth = yearMonth, // 基于结束日期计算年月（格式：YYYY-MM）
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
				throw new ArgumentException("Bill period start date must be earlier than end date.");
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
				throw new InvalidOperationException($"Duplicate bill detected. This bill already exists (ID: {existingBill.Id}, period: {dto.BillPeriodStart:yyyy-MM-dd} to {dto.BillPeriodEnd:yyyy-MM-dd}). Do not add the same bill again.");
			}

			// 3. 计算碳排放
			var carbonResult = await _calculationService.CalculateCarbonEmissionAsync(
				dto.ElectricityUsage,
				dto.WaterUsage,
				dto.GasUsage,
				ct);

			// 4. 创建实体
			// 计算 YearMonth：基于 BillPeriodEnd 的年月，确保月份正确
			var endDate = dto.BillPeriodEnd;
			var yearMonth = $"{endDate.Year:D4}-{endDate.Month:D2}"; // 使用 D2 确保月份是两位数（01-12）
			var periodLabel = yearMonth; // 用于 ActivityLog 的标签
			
			var utilityBill = new UtilityBill
			{
				UserId = userId,
				BillType = dto.BillType,
				BillPeriodStart = dto.BillPeriodStart,
				BillPeriodEnd = endDate,
				YearMonth = yearMonth, // 基于结束日期计算年月（格式：YYYY-MM）
				ElectricityUsage = dto.ElectricityUsage,
				WaterUsage = dto.WaterUsage,
				GasUsage = dto.GasUsage,
				ElectricityCarbonEmission = carbonResult.ElectricityCarbon,
				WaterCarbonEmission = carbonResult.WaterCarbon,
				GasCarbonEmission = carbonResult.GasCarbon,
				TotalCarbonEmission = carbonResult.TotalCarbon,
				InputMethod = InputMethod.Manual,
				Notes = dto.Notes
			};

			// 5. 保存到数据库
			await _db.UtilityBills.AddAsync(utilityBill, ct);
			await _db.SaveChangesAsync(ct);

			_logger.LogInformation("Utility bill created manually: UserId={UserId}, BillId={BillId}", userId, utilityBill.Id);

			// 6. 不再创建 ActivityLog，避免与 UtilityBills 重复。Dashboard/Leaderboard/Records 均直接从 UtilityBills 统计。

			// 7. 更新用户总碳排放
			await UpdateUserTotalCarbonEmissionAsync(userId, ct);

			// 8. 转换为DTO返回
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

		// 同时删除关联的 ActivityLogs（水电账单在创建时会生成 Electricity/Water/Gas 的 ActivityLog）
		// 通过 DetectedLabel 中包含的 YearMonth 和 Utility 类别来匹配
		var yearMonth = bill.YearMonth;
		var utilityActivityLogs = await _db.ActivityLogs
			.Where(l => l.UserId == userId 
				&& l.CarbonReference != null 
				&& l.CarbonReference.Category == CarbonCategory.Utility
				&& l.DetectedLabel != null 
				&& l.DetectedLabel.Contains($"({yearMonth})"))
			.ToListAsync(ct);

		if (utilityActivityLogs.Any())
		{
			_db.ActivityLogs.RemoveRange(utilityActivityLogs);
			_logger.LogInformation("Removing {Count} ActivityLogs for UtilityBill YearMonth={YearMonth}", utilityActivityLogs.Count, yearMonth);
		}

		_db.UtilityBills.Remove(bill);
		await _db.SaveChangesAsync(ct);

		// 更新用户总碳排放
		await UpdateUserTotalCarbonEmissionAsync(userId, ct);

		_logger.LogInformation("Utility bill deleted: UserId={UserId}, BillId={BillId}, YearMonth={YearMonth}", userId, id, yearMonth);

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

		// 总体统计（不含煤气）
		var totalRecords = await baseQuery.CountAsync(ct);
		var totalElectricityUsage = await baseQuery.SumAsync(b => b.ElectricityUsage ?? 0, ct);
		var totalWaterUsage = await baseQuery.SumAsync(b => b.WaterUsage ?? 0, ct);
		var totalCarbonEmission = await baseQuery.SumAsync(b => b.ElectricityCarbonEmission + b.WaterCarbonEmission, ct);

		// 按账单类型分组统计
		var byBillType = await baseQuery
			.GroupBy(b => b.BillType)
			.Select(g => new
			{
				BillType = g.Key,
				RecordCount = g.Count(),
				TotalElectricityUsage = g.Sum(b => b.ElectricityUsage ?? 0),
				TotalWaterUsage = g.Sum(b => b.WaterUsage ?? 0),
				TotalCarbonEmission = g.Sum(b => b.ElectricityCarbonEmission + b.WaterCarbonEmission)
			})
			.ToListAsync(ct);

		var byBillTypeList = byBillType.Select(g => new BillTypeStatisticsDto
		{
			BillType = g.BillType,
			BillTypeName = GetBillTypeName(g.BillType),
			RecordCount = g.RecordCount,
			TotalElectricityUsage = g.TotalElectricityUsage,
			TotalWaterUsage = g.TotalWaterUsage,
			TotalCarbonEmission = g.TotalCarbonEmission
		}).ToList();

		return new UtilityBillStatisticsDto
		{
			TotalRecords = totalRecords,
			TotalElectricityUsage = totalElectricityUsage,
			TotalWaterUsage = totalWaterUsage,
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
			throw new ArgumentException("File cannot be empty.");
		}

		// 文件大小限制（10MB）
		if (file.Length > 10 * 1024 * 1024)
		{
			throw new ArgumentException("File size must not exceed 10MB.");
		}

		// 文件类型验证
		// 注意：虽然允许上传PDF，但Google Vision API不支持直接处理PDF
		// 建议用户将PDF转换为图片（JPG/PNG）后再上传
		var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".pdf" };
		var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
		if (!allowedExtensions.Contains(extension))
		{
			throw new ArgumentException($"Unsupported file type. Supported types: {string.Join(", ", allowedExtensions)}");
		}
	}

	/// <summary>
	/// 将实体转换为响应DTO
	/// </summary>
	private UtilityBillResponseDto ToResponseDto(UtilityBill bill)
	{
		// 接口不返回煤气；总排放仅含电+水
		var totalCarbon = bill.ElectricityCarbonEmission + bill.WaterCarbonEmission;
		return new UtilityBillResponseDto
		{
			Id = bill.Id,
			BillType = bill.BillType,
			BillTypeName = GetBillTypeName(bill.BillType),
			BillPeriodStart = bill.BillPeriodStart,
			BillPeriodEnd = bill.BillPeriodEnd,
			ElectricityUsage = bill.ElectricityUsage,
			WaterUsage = bill.WaterUsage,
			ElectricityCarbonEmission = bill.ElectricityCarbonEmission,
			WaterCarbonEmission = bill.WaterCarbonEmission,
			TotalCarbonEmission = totalCarbon,
			InputMethod = bill.InputMethod,
			InputMethodName = GetInputMethodName(bill.InputMethod),
			OcrConfidence = bill.OcrConfidence,
			OcrRawText = bill.OcrRawText,
			Notes = bill.Notes,
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
			UtilityBillType.Electricity => "Electricity",
			UtilityBillType.Water => "Water",
			UtilityBillType.Gas => "Gas",
			UtilityBillType.Combined => "Combined",
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
			InputMethod.Auto => "Auto",
			InputMethod.Manual => "Manual",
			_ => inputMethod.ToString()
		};
	}

	/// <summary>
	/// 更新用户总碳排放（从 ActivityLogs、TravelLogs、UtilityBills 汇总）
	/// </summary>
	private async Task UpdateUserTotalCarbonEmissionAsync(int userId, CancellationToken ct)
	{
		try
		{
			var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
			if (user == null) return;

			var activityEmission = await _db.ActivityLogs
				.Where(a => a.UserId == userId)
				.SumAsync(a => (decimal?)a.TotalEmission, ct) ?? 0m;

			var travelEmission = await _db.TravelLogs
				.Where(t => t.UserId == userId)
				.SumAsync(t => (decimal?)t.CarbonEmission, ct) ?? 0m;

			var utilityEmission = await _db.UtilityBills
				.Where(u => u.UserId == userId)
				.SumAsync(u => (decimal?)u.TotalCarbonEmission, ct) ?? 0m;

			user.TotalCarbonEmission = activityEmission + travelEmission + utilityEmission;
			await _db.SaveChangesAsync(ct);
		}
		catch (Exception ex)
		{
			// 记录错误但不影响主流程
			_logger.LogError(ex, "Failed to update TotalCarbonEmission for user {UserId}", userId);
		}
	}
}
