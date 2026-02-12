using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.Models;

[Table("ApplicationUsers")]
public class ApplicationUser : BaseEntity
{
	[Required]
	[MaxLength(100)]
	public string Username { get; set; } = string.Empty;

	// 展示昵称（可与 Username 不同；允许重复）
	[MaxLength(100)]
	public string? Nickname { get; set; }

	[Required]
	[MaxLength(256)]
	public string Email { get; set; } = string.Empty;

	[Required]
	[MaxLength(512)]
	public string PasswordHash { get; set; } = string.Empty;

	[Required]
	public UserRole Role { get; set; } = UserRole.User;

	// AvatarUrl 存储 Base64 编码的图片数据，格式：data:image/{type};base64,{base64字符串}
	// 使用 nvarchar(max) 以支持大图片
	public string? AvatarUrl { get; set; }

	[Column(TypeName = "decimal(18,2)")]
	public decimal TotalCarbonSaved { get; set; }

	/// <summary>
	/// 用户总碳排放量（kg CO2e）
	/// 包括：ActivityLogs 的 TotalEmission + TravelLogs 的 CarbonEmission + UtilityBills 的 TotalCarbonEmission
	/// </summary>
	[Column(TypeName = "decimal(18,4)")]
	public decimal TotalCarbonEmission { get; set; }

	public int CurrentPoints { get; set; }

	/// <summary>
	/// 兼容部分接口描述中的字段名：Points（等同于 CurrentPoints）。
	/// </summary>
	[NotMapped]
	public int Points
	{
		get => CurrentPoints;
		set => CurrentPoints = value;
	}

	[MaxLength(100)]
	[Required]
	public string Region { get; set; } = string.Empty;

	[Required]
	public DateTime BirthDate { get; set; }

	/// <summary>
	/// 最近一次达成“碳中和日”的日期（按 UTC 日期存储）。
	/// </summary>
	public DateTime? LastNeutralDate { get; set; }

	/// <summary>
	/// 连续达成“碳中和日”的天数（从 1 开始累计，未达成则在下次达成时重置为 1）。
	/// </summary>
	public int ContinuousNeutralDays { get; set; }

	/// <summary>
	/// 当日已用于树木生长消耗的步数（每天重置）。
	/// </summary>
	public int StepsUsedToday { get; set; }

	/// <summary>
	/// 最近一次消耗步数用于树木生长的日期（按 UTC 日期的 date 部分存储，用于每日重置）。
	/// </summary>
	public DateTime? LastStepUsageDate { get; set; }

	public bool IsActive { get; set; } = true;

	// 植树相关：总树数与当前树生长进度（0-100）
	public int TreesTotalCount { get; set; }
	public int CurrentTreeProgress { get; set; }

	public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
	public ICollection<AiInsight> AiInsights { get; set; } = new List<AiInsight>();
	public ICollection<StepRecord> StepRecords { get; set; } = new List<StepRecord>();
}

