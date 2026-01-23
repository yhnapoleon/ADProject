using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.DTOs.UtilityBill;

/// <summary>
/// 上传账单文件的请求 DTO
/// </summary>
public class UploadUtilityBillDto
{
	/// <summary>
	/// 账单文件（支持图片：JPG、PNG、GIF、BMP、WEBP，或PDF文件）
	/// </summary>
	[Required(ErrorMessage = "File is required")]
	public IFormFile File { get; set; } = null!;
}
