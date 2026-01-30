using System.Globalization;
using System.Text.RegularExpressions;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.Services;

/// <summary>
/// 水电账单数据提取服务实现
/// </summary>
public class UtilityBillParser : IUtilityBillParser
{
	private readonly ILogger<UtilityBillParser> _logger;

	public UtilityBillParser(ILogger<UtilityBillParser> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// 从OCR识别的文本中提取账单数据
	/// </summary>
	public async Task<ExtractedBillData?> ParseBillDataAsync(string ocrText, UtilityBillType? expectedType = null, CancellationToken ct = default)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(ocrText))
			{
				_logger.LogWarning("OCR text is empty");
				return null;
			}

			// 转换为小写以便匹配（保留原始文本用于提取）
			var lowerText = ocrText.ToLowerInvariant();

			// 1. 识别账单类型
			var billType = expectedType ?? IdentifyBillType(lowerText);

			// 2. 提取用量数据
			var electricityUsage = ExtractElectricityUsage(ocrText);
			var waterUsage = ExtractWaterUsage(ocrText);
			var gasUsage = ExtractGasUsage(ocrText);

			// 3. 提取账单周期
			var (startDate, endDate) = ExtractBillPeriod(ocrText);

			// 4. 计算置信度
			var confidence = CalculateConfidence(billType, electricityUsage, waterUsage, gasUsage, startDate, endDate);

			var result = new ExtractedBillData
			{
				BillType = billType,
				ElectricityUsage = electricityUsage,
				WaterUsage = waterUsage,
				GasUsage = gasUsage,
				BillPeriodStart = startDate,
				BillPeriodEnd = endDate,
				Confidence = confidence
			};

			_logger.LogInformation(
				"Parsed bill data: Type={BillType}, Electricity={Electricity}, Water={Water}, Gas={Gas}, Period={Start} to {End}, Confidence={Confidence}",
				billType, electricityUsage, waterUsage, gasUsage, startDate, endDate, confidence);

			return await Task.FromResult(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error parsing bill data from OCR text");
			return null;
		}
	}

	/// <summary>
	/// 识别账单类型
	/// </summary>
	private UtilityBillType IdentifyBillType(string lowerText)
	{
		// 增强的新加坡水电账单关键词
		var hasElectricity = ContainsKeywords(lowerText, new[]
		{
			"electricity", "electric", "power", "kwh", "kw·h", "kw h", "kilowatt",
			"sp group", "sp services", "singapore power", "electricity services",
			"consumption", "usage", "units consumed", "energy consumption"
		});

		var hasWater = ContainsKeywords(lowerText, new[]
		{
			"water", "pub", "public utilities board", "water services",
			"m³", "m3", "cubic meter", "cubic metre", "cu m", "cu.m",
			"litre", "liter", "l", "water consumption", "water usage"
		});

		var hasGas = ContainsKeywords(lowerText, new[]
		{
			"gas", "town gas", "natural gas", "city gas", "gas consumption", "gas usage"
		});

		if (hasElectricity && hasWater && hasGas)
			return UtilityBillType.Combined;
		if (hasElectricity && hasWater)
			return UtilityBillType.Combined;
		if (hasElectricity && hasGas)
			return UtilityBillType.Combined;
		if (hasWater && hasGas)
			return UtilityBillType.Combined;
		if (hasElectricity)
			return UtilityBillType.Electricity;
		if (hasWater)
			return UtilityBillType.Water;
		if (hasGas)
			return UtilityBillType.Gas;

		// 默认返回综合账单
		return UtilityBillType.Combined;
	}

	/// <summary>
	/// 检查文本是否包含关键词
	/// </summary>
	private bool ContainsKeywords(string text, string[] keywords)
	{
		return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// 提取用电量（kWh）
	/// </summary>
	private decimal? ExtractElectricityUsage(string text)
	{
		// 匹配模式：数字 + kWh/kW·h/kwh
		var patterns = new[]
		{
			@"(\d+\.?\d*)\s*(?:kwh|kw·h|kwh)",
			@"(?:consumption|usage|used)\s*:?\s*(\d+\.?\d*)\s*(?:kwh|kw·h)",
			@"(\d+\.?\d*)\s*(?:kwh|kw·h)\s*(?:consumption|usage)"
		};

		foreach (var pattern in patterns)
		{
			var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
			if (match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
			{
				return value;
			}
		}

		return null;
	}

	/// <summary>
	/// 提取用水量（m³，如果单位是L则转换为m³）
	/// </summary>
	private decimal? ExtractWaterUsage(string text)
	{
		// 匹配模式：数字 + m³/m3/cubic meter/Cu M/L/litre
		// 增强对 "Cu M" 格式的支持（SP Services 账单格式）
		var patterns = new[]
		{
			@"(\d+\.?\d*)\s*(?:m³|m3|cubic\s*meter|cu\s*m|cu\.?\s*m\.?|cu\s+m)",
			@"(?:consumption|usage|used)\s*:?\s*(\d+\.?\d*)\s*(?:m³|m3|cubic\s*meter|cu\s*m|cu\.?\s*m\.?|cu\s+m)",
			@"(\d+\.?\d*)\s*(?:m³|m3|cu\s*m|cu\.?\s*m\.?|cu\s+m)\s*(?:consumption|usage)",
			// SP Services 特定格式：Usage: 7.6 Cu M
			@"usage\s*:?\s*(\d+\.?\d*)\s*cu\s+m",
			@"(\d+\.?\d*)\s*cu\s+m\s*usage"
		};

		foreach (var pattern in patterns)
		{
			var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
			if (match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
			{
				return value;
			}
		}

		// 尝试匹配升（L），需要转换为m³
		var litrePatterns = new[]
		{
			@"(\d+\.?\d*)\s*(?:l|litre|liter|litres|liters)",
			@"(?:consumption|usage|used)\s*:?\s*(\d+\.?\d*)\s*(?:l|litre|liter)"
		};

		foreach (var pattern in litrePatterns)
		{
			var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
			if (match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
			{
				// 转换为m³：1 m³ = 1000 L
				return value / 1000m;
			}
		}

		return null;
	}

	/// <summary>
	/// 提取用气量（kWh 或 m³）
	/// </summary>
	private decimal? ExtractGasUsage(string text)
	{
		// 匹配模式：数字 + kWh（燃气）或 m³（燃气）
		// 需要确保是在燃气相关的上下文中
		var patterns = new[]
		{
			@"(?:gas|town\s*gas|natural\s*gas|city\s*gas).*?(\d+\.?\d*)\s*(?:kwh|kw·h|m³|m3)",
			@"(\d+\.?\d*)\s*(?:kwh|kw·h|m³|m3).*?(?:gas|town\s*gas|natural\s*gas|city\s*gas)",
			@"(?:gas|town\s*gas).*?(?:consumption|usage|used)\s*:?\s*(\d+\.?\d*)\s*(?:kwh|kw·h|m³|m3)"
		};

		foreach (var pattern in patterns)
		{
			var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
			if (match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
			{
				return value;
			}
		}

		return null;
	}

	/// <summary>
	/// 提取账单周期（开始日期和结束日期）
	/// </summary>
	private (DateTime? startDate, DateTime? endDate) ExtractBillPeriod(string text)
	{
		DateTime? startDate = null;
		DateTime? endDate = null;

		_logger.LogDebug("Extracting bill period from text (length: {Length})", text.Length);
		
		// 记录OCR文本的前500个字符用于调试
		var preview = text.Length > 500 ? text.Substring(0, 500) : text;
		_logger.LogDebug("OCR text preview: {Preview}", preview);

		// 优先尝试匹配 "Billing Period: DD MMM YYYY - DD MMM YYYY" 格式（例如：05 Nov 2025 - 05 Dec 2025）
		// 使用更严格的模式，确保年份是4位数（2000-2099），并且使用单词边界
		var periodPattern = @"(?:billing\s+period|period)\s*:?\s*(\d{1,2})\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s+(20\d{2})\s*[-–—]\s*(\d{1,2})\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s+(20\d{2})\b";
		var periodMatch = Regex.Match(text, periodPattern, RegexOptions.IgnoreCase);
		if (periodMatch.Success && periodMatch.Groups.Count >= 7)
		{
			_logger.LogDebug("Matched billing period pattern: {Match}", periodMatch.Value);
			if (TryParseDateWithMonthName(periodMatch.Groups[1].Value, periodMatch.Groups[2].Value, periodMatch.Groups[3].Value, out var start))
			{
				startDate = start;
				_logger.LogDebug("Parsed start date: {Date}", start);
			}
			if (TryParseDateWithMonthName(periodMatch.Groups[4].Value, periodMatch.Groups[5].Value, periodMatch.Groups[6].Value, out var end))
			{
				endDate = end;
				_logger.LogDebug("Parsed end date: {Date}", end);
			}
		}
		
		// 如果上面的模式没有匹配，尝试从 "DD MMM YYYY - DD MMM YYYY" 格式提取（例如：05 Nov 2025 - 05 Dec 2025）
		// 使用更严格的模式，确保年份是4位数（2000-2099），并且两个日期之间有分隔符
		if (!startDate.HasValue || !endDate.HasValue)
		{
			// 先尝试匹配带分隔符的格式，年份必须是20xx格式
			var rangePattern = @"(\d{1,2})\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s+(20\d{2})\s*[-–—]\s*(\d{1,2})\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s+(20\d{2})\b";
			var rangeMatch = Regex.Match(text, rangePattern, RegexOptions.IgnoreCase);
			if (rangeMatch.Success && rangeMatch.Groups.Count >= 7)
			{
				_logger.LogDebug("Matched date range pattern: {Match}", rangeMatch.Value);
				if (!startDate.HasValue && TryParseDateWithMonthName(rangeMatch.Groups[1].Value, rangeMatch.Groups[2].Value, rangeMatch.Groups[3].Value, out var start))
				{
					startDate = start;
					_logger.LogDebug("Parsed start date from range: {Date}", start);
				}
				if (!endDate.HasValue && TryParseDateWithMonthName(rangeMatch.Groups[4].Value, rangeMatch.Groups[5].Value, rangeMatch.Groups[6].Value, out var end))
				{
					endDate = end;
					_logger.LogDebug("Parsed end date from range: {Date}", end);
				}
			}
			
			// 如果还是没有匹配，尝试匹配单独的日期，年份必须是20xx格式
			if (!startDate.HasValue || !endDate.HasValue)
			{
				var monthNamePattern = @"\b(\d{1,2})\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s+(20\d{2})\b";
				var monthMatches = Regex.Matches(text, monthNamePattern, RegexOptions.IgnoreCase);
				if (monthMatches.Count >= 2)
				{
					var dates = new List<DateTime>();
					foreach (Match match in monthMatches)
					{
						if (match.Groups.Count >= 4 && TryParseDateWithMonthName(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, out var date))
						{
							dates.Add(date);
						}
					}
					if (dates.Count >= 2)
					{
						var sortedDates = dates.OrderBy(d => d).ToList();
						if (!startDate.HasValue) 
						{
							startDate = sortedDates[0];
							_logger.LogDebug("Parsed start date from separate dates: {Date}", startDate);
						}
						if (!endDate.HasValue) 
						{
							endDate = sortedDates[sortedDates.Count - 1];
							_logger.LogDebug("Parsed end date from separate dates: {Date}", endDate);
						}
					}
				}
			}
		}

		// 如果月份名称格式都没有匹配，尝试数字格式
		if (!startDate.HasValue || !endDate.HasValue)
		{
			// 尝试从特定格式中提取（例如：From 01/01/2024 to 31/01/2024），年份必须是20xx
			var fromToPattern = @"(?:from|start)\s+(\d{1,2})[/\-\.](\d{1,2})[/\-\.](20\d{2})\s+(?:to|end)\s+(\d{1,2})[/\-\.](\d{1,2})[/\-\.](20\d{2})";
			var fromToMatch = Regex.Match(text, fromToPattern, RegexOptions.IgnoreCase);
			if (fromToMatch.Success)
			{
				_logger.LogDebug("Matched from-to pattern: {Match}", fromToMatch.Value);
				if (!startDate.HasValue && TryParseDate(fromToMatch.Groups[1], fromToMatch.Groups[2], fromToMatch.Groups[3], out var start))
				{
					startDate = start;
					_logger.LogDebug("Parsed start date from from-to: {Date}", start);
				}
				if (!endDate.HasValue && TryParseDate(fromToMatch.Groups[4], fromToMatch.Groups[5], fromToMatch.Groups[6], out var end))
				{
					endDate = end;
					_logger.LogDebug("Parsed end date from from-to: {Date}", end);
				}
			}
			
			// 最后尝试匹配所有数字格式的日期，但只匹配20xx年份
			// 注意：这个模式可能会匹配到错误的数字组合，所以只在其他方法都失败时才使用
			if (!startDate.HasValue || !endDate.HasValue)
			{
				var dateMatches = new List<DateTime>();

				// DD/MM/YYYY 格式 (Group[1]=day, Group[2]=month, Group[3]=year)
				var ddMmYyyyPattern = @"\b(\d{1,2})[/\-\.](\d{1,2})[/\-\.](20\d{2})\b";
				var ddMmYyyyMatches = Regex.Matches(text, ddMmYyyyPattern, RegexOptions.IgnoreCase);
				foreach (Match match in ddMmYyyyMatches)
				{
					_logger.LogDebug("Matched DD/MM/YYYY pattern: {Match}", match.Value);
					if (match.Groups.Count >= 4 && TryParseDate(match.Groups[1], match.Groups[2], match.Groups[3], out var date))
					{
						dateMatches.Add(date);
						_logger.LogDebug("Successfully parsed date: {Date}", date);
					}
					else
					{
						_logger.LogWarning("Failed to parse date from match: {Match}", match.Value);
					}
				}

				// YYYY-MM-DD 格式 (Group[1]=year, Group[2]=month, Group[3]=day)
				var yyyyMmDdPattern = @"\b(20\d{2})[/\-\.](\d{1,2})[/\-\.](\d{1,2})\b";
				var yyyyMmDdMatches = Regex.Matches(text, yyyyMmDdPattern, RegexOptions.IgnoreCase);
				foreach (Match match in yyyyMmDdMatches)
				{
					_logger.LogDebug("Matched YYYY-MM-DD pattern: {Match}", match.Value);
					// 注意：对于YYYY-MM-DD格式，组顺序是 [year, month, day]，需要调整参数顺序
					if (match.Groups.Count >= 4 && TryParseDate(match.Groups[3], match.Groups[2], match.Groups[1], out var date))
					{
						dateMatches.Add(date);
						_logger.LogDebug("Successfully parsed date: {Date}", date);
					}
					else
					{
						_logger.LogWarning("Failed to parse date from match: {Match}", match.Value);
					}
				}

				// 如果有两个日期，通常第一个是开始日期，第二个是结束日期
				if (dateMatches.Count >= 2)
				{
					var sortedDates = dateMatches.OrderBy(d => d).ToList();
					// 再次验证日期年份（防止无效日期被使用）
					var validDates = sortedDates.Where(d => d.Year >= 2000 && d.Year <= 2100).ToList();
					if (validDates.Count >= 2)
					{
						if (!startDate.HasValue) 
						{
							startDate = validDates[0];
							_logger.LogDebug("Set start date from numeric format: {Date}", startDate);
						}
						if (!endDate.HasValue) 
						{
							endDate = validDates[validDates.Count - 1];
							_logger.LogDebug("Set end date from numeric format: {Date}", endDate);
						}
					}
					else if (validDates.Count == 1 && !endDate.HasValue)
					{
						endDate = validDates[0];
						_logger.LogDebug("Set end date from single valid numeric format match: {Date}", endDate);
					}
					else
					{
						_logger.LogWarning("No valid dates found in {Count} matches (all have invalid years)", dateMatches.Count);
					}
				}
				else if (dateMatches.Count == 1 && !endDate.HasValue)
				{
					var date = dateMatches[0];
					// 再次验证日期年份
					if (date.Year >= 2000 && date.Year <= 2100)
					{
						endDate = date;
						_logger.LogDebug("Set end date from single numeric format match: {Date}", endDate);
					}
					else
					{
						_logger.LogWarning("Rejected invalid date from single match: {Date} (Year: {Year})", date, date.Year);
					}
				}
			}
		}

		_logger.LogInformation("Extracted bill period: Start={StartDate}, End={EndDate}", startDate, endDate);
		
		// 最终验证：如果日期存在但年份无效，清除这些日期并记录错误
		if (startDate.HasValue && (startDate.Value.Year < 2000 || startDate.Value.Year > 2100))
		{
			_logger.LogError("CRITICAL: Invalid start date year detected: {Year} (Date: {Date}) - CLEARING DATE", startDate.Value.Year, startDate.Value);
			startDate = null;
		}
		if (endDate.HasValue && (endDate.Value.Year < 2000 || endDate.Value.Year > 2100))
		{
			_logger.LogError("CRITICAL: Invalid end date year detected: {Year} (Date: {Date}) - CLEARING DATE", endDate.Value.Year, endDate.Value);
			endDate = null;
		}
		
		_logger.LogInformation("Final extracted bill period after validation: Start={StartDate}, End={EndDate}", startDate, endDate);
		return (startDate, endDate);
	}

	/// <summary>
	/// 尝试解析日期
	/// </summary>
	private bool TryParseDate(Match match, out DateTime date)
	{
		if (match.Groups.Count >= 4)
		{
			return TryParseDate(match.Groups[1], match.Groups[2], match.Groups[3], out date);
		}
		date = default;
		return false;
	}

	/// <summary>
	/// 尝试解析日期（从三个组）
	/// </summary>
	private bool TryParseDate(Group dayGroup, Group monthGroup, Group yearGroup, out DateTime date)
	{
		date = default;
		
		if (dayGroup == null || monthGroup == null || yearGroup == null)
		{
			return false;
		}
		
		// 验证年份长度：必须是2位或4位数字，且4位数字必须以20开头
		if (yearGroup.Value.Length == 4 && !yearGroup.Value.StartsWith("20"))
		{
			_logger.LogWarning("Invalid 4-digit year (must start with 20): {Year}", yearGroup.Value);
			return false;
		}
		
		if (yearGroup.Value.Length > 4)
		{
			_logger.LogWarning("Year too long: {Year}", yearGroup.Value);
			return false;
		}
		
		if (!int.TryParse(dayGroup.Value, out var day) ||
			!int.TryParse(monthGroup.Value, out var month) ||
			!int.TryParse(yearGroup.Value, out var year))
		{
			_logger.LogWarning("Failed to parse date components: day={Day}, month={Month}, year={Year}", dayGroup.Value, monthGroup.Value, yearGroup.Value);
			return false;
		}

		// 处理年份缩写（例如：24 -> 2024）
		if (year < 100)
		{
			// 如果年份小于50，假设是2000-2049；如果大于等于50，假设是1950-1999
			year += year < 50 ? 2000 : 1900;
		}
		
		// 验证年份范围（合理范围：2000-2100）
		if (year < 2000 || year > 2100)
		{
			_logger.LogWarning("Year out of range: {Year} (from groups: day={Day}, month={Month}, year={Year})", year, dayGroup.Value, monthGroup.Value, yearGroup.Value);
			return false;
		}

		// 验证月份和日期范围
		if (month < 1 || month > 12 || day < 1 || day > 31)
		{
			_logger.LogWarning("Month or day out of range: month={Month}, day={Day}", month, day);
			return false;
		}

		// 再次验证年份范围（双重检查，防止DateTime.TryParseExact接受无效年份）
		if (year < 2000 || year > 2100)
		{
			_logger.LogWarning("Year validation failed before DateTime creation: {Year}", year);
			return false;
		}
		
		// 尝试解析为 DD/MM/YYYY 或 MM/DD/YYYY
		if (DateTime.TryParseExact($"{day:D2}/{month:D2}/{year}", "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
		{
			// 再次验证创建的日期年份（防止DateTime.TryParseExact接受无效年份）
			if (date.Year < 2000 || date.Year > 2100)
			{
				_logger.LogWarning("Created date has invalid year: {Year} (from {Day}/{Month}/{Year})", date.Year, day, month, year);
				return false;
			}
			_logger.LogDebug("Successfully parsed date: {Date} from {Day}/{Month}/{Year}", date, day, month, year);
			return true;
		}
		if (DateTime.TryParseExact($"{month:D2}/{day:D2}/{year}", "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
		{
			// 再次验证创建的日期年份（防止DateTime.TryParseExact接受无效年份）
			if (date.Year < 2000 || date.Year > 2100)
			{
				_logger.LogWarning("Created date has invalid year: {Year} (from {Month}/{Day}/{Year})", date.Year, month, day, year);
				return false;
			}
			_logger.LogDebug("Successfully parsed date: {Date} from {Month}/{Day}/{Year}", date, month, day, year);
			return true;
		}

		_logger.LogWarning("Failed to parse date: {Day}/{Month}/{Year}", day, month, year);
		return false;
	}

	/// <summary>
	/// 尝试解析日期（包含月份名称，例如：05 Nov 2025）
	/// </summary>
	private bool TryParseDateWithMonthName(Match match, out DateTime date)
	{
		if (match.Groups.Count >= 4)
		{
			return TryParseDateWithMonthName(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, out date);
		}
		date = default;
		return false;
	}

	/// <summary>
	/// 尝试解析日期（包含月份名称）
	/// </summary>
	private bool TryParseDateWithMonthName(string dayStr, string monthStr, string yearStr, out DateTime date)
	{
		date = default;
		
		// 验证输入不为空
		if (string.IsNullOrWhiteSpace(dayStr) || string.IsNullOrWhiteSpace(monthStr) || string.IsNullOrWhiteSpace(yearStr))
		{
			_logger.LogWarning("Empty date components: day={Day}, month={Month}, year={Year}", dayStr, monthStr, yearStr);
			return false;
		}
		
		// 验证年份字符串格式：必须是2位或4位数字，且4位数字必须以20开头
		if (yearStr.Length == 4 && !yearStr.StartsWith("20"))
		{
			_logger.LogWarning("Invalid 4-digit year (must start with 20): {Year}", yearStr);
			return false;
		}
		
		if (yearStr.Length > 4)
		{
			_logger.LogWarning("Year string too long: {Year}", yearStr);
			return false;
		}
		
		if (!int.TryParse(dayStr, out var day))
		{
			_logger.LogWarning("Failed to parse day: {Day}", dayStr);
			return false;
		}
		
		if (!int.TryParse(yearStr, out var year))
		{
			_logger.LogWarning("Failed to parse year: {Year}", yearStr);
			return false;
		}

		// 处理年份：如果是2位数，转换为4位数（例如：25 -> 2025）
		if (year < 100)
		{
			// 如果年份小于50，假设是2000-2049；如果大于等于50，假设是1950-1999
			year += year < 50 ? 2000 : 1900;
		}
		
		// 验证年份范围（合理范围：2000-2100）
		if (year < 2000 || year > 2100)
		{
			_logger.LogWarning("Year out of range: {Year} (from: day={Day}, month={Month}, year={Year})", year, dayStr, monthStr, yearStr);
			return false;
		}

		// 月份名称映射
		var monthMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			{ "jan", 1 }, { "january", 1 },
			{ "feb", 2 }, { "february", 2 },
			{ "mar", 3 }, { "march", 3 },
			{ "apr", 4 }, { "april", 4 },
			{ "may", 5 },
			{ "jun", 6 }, { "june", 6 },
			{ "jul", 7 }, { "july", 7 },
			{ "aug", 8 }, { "august", 8 },
			{ "sep", 9 }, { "september", 9 },
			{ "oct", 10 }, { "october", 10 },
			{ "nov", 11 }, { "november", 11 },
			{ "dec", 12 }, { "december", 12 }
		};

		// 提取月份名称的前3个字符并查找
		var monthKey = monthStr.Length >= 3 ? monthStr.Substring(0, 3).ToLowerInvariant() : monthStr.ToLowerInvariant();
		if (!monthMap.TryGetValue(monthKey, out var month))
		{
			return false;
		}

		// 再次验证年份范围（双重检查，防止new DateTime接受无效年份）
		if (year < 2000 || year > 2100)
		{
			_logger.LogWarning("Year validation failed before DateTime creation: {Year} (from: day={Day}, month={Month}, year={Year})", year, dayStr, monthStr, yearStr);
			return false;
		}
		
		// 验证日期有效性
		try
		{
			date = new DateTime(year, month, day);
			
			// 再次验证创建的日期年份（防止new DateTime接受无效年份）
			if (date.Year < 2000 || date.Year > 2100)
			{
				_logger.LogWarning("Created date has invalid year: {Year} (from: day={Day}, month={Month}, year={Year})", date.Year, dayStr, monthStr, yearStr);
				return false;
			}
			
			_logger.LogDebug("Successfully parsed date: {Date} from {Day} {Month} {Year}", date, dayStr, monthStr, yearStr);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to create DateTime from: day={Day}, month={Month}, year={Year}", dayStr, monthStr, yearStr);
			return false;
		}
	}

	/// <summary>
	/// 计算提取置信度
	/// </summary>
	private decimal CalculateConfidence(
		UtilityBillType billType,
		decimal? electricityUsage,
		decimal? waterUsage,
		decimal? gasUsage,
		DateTime? startDate,
		DateTime? endDate)
	{
		var score = 0m;
		var maxScore = 0m;

		// 账单类型识别（20分）
		maxScore += 20;
		if (billType != UtilityBillType.Combined || (electricityUsage.HasValue || waterUsage.HasValue || gasUsage.HasValue))
		{
			score += 20;
		}
		else
		{
			score += 10; // 部分分数
		}

		// 用量数据（根据账单类型，最多40分）
		maxScore += 40;
		if (billType == UtilityBillType.Electricity || billType == UtilityBillType.Combined)
		{
			if (electricityUsage.HasValue)
				score += 20;
		}
		if (billType == UtilityBillType.Water || billType == UtilityBillType.Combined)
		{
			if (waterUsage.HasValue)
				score += 20;
		}
		if (billType == UtilityBillType.Gas || billType == UtilityBillType.Combined)
		{
			if (gasUsage.HasValue)
				score += 20;
		}

		// 日期数据（40分）
		maxScore += 40;
		if (startDate.HasValue && endDate.HasValue)
		{
			score += 40;
		}
		else if (startDate.HasValue || endDate.HasValue)
		{
			score += 20; // 部分分数
		}

		// 计算置信度（0-1）
		if (maxScore == 0)
			return 0;

		return Math.Min(1m, score / maxScore);
	}
}
