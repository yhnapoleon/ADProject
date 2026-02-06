using System.Text.RegularExpressions;

namespace EcoLens.Api.Services;

/// <summary>
/// 文档类型分类器实现
/// 专门针对新加坡水电账单进行识别
/// </summary>
public class DocumentTypeClassifier : IDocumentTypeClassifier
{
	private readonly ILogger<DocumentTypeClassifier> _logger;

	// 新加坡水电账单关键词（英文）
	private static readonly string[] UtilityBillKeywords = new[]
	{
		// 电费相关
		"electricity", "electric", "power", "kwh", "kw·h", "kilowatt",
		"sp group", "sp services", "singapore power", "electricity services",
		"consumption", "usage", "units consumed",
		
		// 水费相关
		"water", "pub", "public utilities board", "water services",
		"m³", "m3", "cubic meter", "cubic metre", "cu m", "cu.m",
		"litre", "liter", "l", "water consumption",
		
		// 燃气相关
		"gas", "town gas", "natural gas", "city gas", "gas consumption",
		
		// 通用账单关键词
		"utility", "utilities", "bill", "statement", "account",
		"billing period", "bill period", "period from", "period to",
		"due date", "amount due", "total amount", "charges",
		"account number", "account no", "service account"
	};

	// 机票/登机牌关键词
	private static readonly string[] FlightTicketKeywords = new[]
	{
		"flight", "airline", "ticket", "boarding pass", "boarding card",
		"departure", "arrival", "gate", "seat", "flight number",
		"passenger", "pnr", "booking reference", "check-in",
		"airport", "terminal", "aircraft", "cabin class",
		"sq", "sia", "singapore airlines", "jetstar", "scoot"
	};

	// 收据关键词
	private static readonly string[] ReceiptKeywords = new[]
	{
		"receipt", "thank you", "change", "cash", "card payment",
		"subtotal", "tax", "gst", "total", "amount paid",
		"item", "quantity", "price", "store", "shop"
	};

	// 发票关键词
	private static readonly string[] InvoiceKeywords = new[]
	{
		"invoice", "tax invoice", "commercial invoice",
		"invoice number", "invoice no", "invoice date",
		"gst registration", "company", "business"
	};

	// 账单特征模式（正则表达式）
	private static readonly Regex[] UtilityBillPatterns = new[]
	{
		new Regex(@"\d+\.?\d*\s*(?:kwh|kw·h|kw h)", RegexOptions.IgnoreCase), // 电量模式
		new Regex(@"\d+\.?\d*\s*(?:m³|m3|cubic\s*meter|cubic\s*metre|cu\.?\s*m)", RegexOptions.IgnoreCase), // 水量模式
		new Regex(@"billing\s+period|bill\s+period|period\s+from|period\s+to", RegexOptions.IgnoreCase), // 账单周期
		new Regex(@"sp\s+group|singapore\s+power|pub\s+singapore", RegexOptions.IgnoreCase), // 新加坡公用事业公司
		new Regex(@"account\s+(?:number|no\.?)\s*:?\s*\d+", RegexOptions.IgnoreCase), // 账户号
	};

	public DocumentTypeClassifier(ILogger<DocumentTypeClassifier> logger)
	{
		_logger = logger;
	}

	public Task<DocumentTypeClassificationResult> ClassifyAsync(string ocrText, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(ocrText))
		{
			return Task.FromResult(new DocumentTypeClassificationResult
			{
				DocumentType = DocumentType.Unknown,
				ConfidenceScore = 0,
				ErrorMessage = "OCR text is empty. Unable to identify document type."
			});
		}

		var lowerText = ocrText.ToLowerInvariant();
		var matchedKeywords = new List<string>();

		// 1. 计算水电账单得分
		var utilityScore = CalculateUtilityBillScore(lowerText, matchedKeywords);

		// 2. 计算其他文档类型得分
		var flightScore = CountMatches(lowerText, FlightTicketKeywords);
		var receiptScore = CountMatches(lowerText, ReceiptKeywords);
		var invoiceScore = CountMatches(lowerText, InvoiceKeywords);

		// 3. 判断文档类型
		var maxScore = Math.Max(Math.Max(utilityScore, flightScore), Math.Max(receiptScore, invoiceScore));

		DocumentType documentType;
		string? errorMessage = null;

		if (utilityScore >= 3) // 阈值：至少匹配3个特征
		{
			documentType = DocumentType.UtilityBill;
		}
		else if (flightScore >= 2)
		{
			documentType = DocumentType.FlightTicket;
			errorMessage = "The uploaded image is a flight ticket/boarding pass, not a utility bill. Please upload a Singapore electricity, water or gas bill.";
		}
		else if (receiptScore >= 3)
		{
			documentType = DocumentType.Receipt;
			errorMessage = "The uploaded image is a receipt, not a utility bill. Please upload a Singapore electricity, water or gas bill.";
		}
		else if (invoiceScore >= 2)
		{
			documentType = DocumentType.Invoice;
			errorMessage = "The uploaded image is an invoice, not a utility bill. Please upload a Singapore electricity, water or gas bill.";
		}
		else
		{
			documentType = DocumentType.Unknown;
			errorMessage = "Unable to identify the uploaded image type. Please ensure you upload a Singapore electricity, water or gas bill.";
		}

		var result = new DocumentTypeClassificationResult
		{
			DocumentType = documentType,
			ConfidenceScore = utilityScore * 10, // 转换为0-100的分数
			MatchedKeywords = matchedKeywords,
			ErrorMessage = errorMessage
		};

		_logger.LogInformation(
			"Document classification: Type={DocumentType}, UtilityScore={UtilityScore}, FlightScore={FlightScore}, ReceiptScore={ReceiptScore}, InvoiceScore={InvoiceScore}",
			documentType, utilityScore, flightScore, receiptScore, invoiceScore);

		return Task.FromResult(result);
	}

	/// <summary>
	/// 计算水电账单得分
	/// </summary>
	private int CalculateUtilityBillScore(string lowerText, List<string> matchedKeywords)
	{
		var score = 0;

		// 1. 关键词匹配（每个关键词1分）
		foreach (var keyword in UtilityBillKeywords)
		{
			if (lowerText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
			{
				score++;
				matchedKeywords.Add(keyword);
			}
		}

		// 2. 模式匹配（每个模式2分）
		foreach (var pattern in UtilityBillPatterns)
		{
			if (pattern.IsMatch(lowerText))
			{
				score += 2;
			}
		}

		return score;
	}

	/// <summary>
	/// 计算匹配的关键词数量
	/// </summary>
	private int CountMatches(string text, string[] keywords)
	{
		return keywords.Count(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
	}
}
