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

	[Required]
	[MaxLength(256)]
	public string Email { get; set; } = string.Empty;

	[Required]
	[MaxLength(512)]
	public string PasswordHash { get; set; } = string.Empty;

	[Required]
	public UserRole Role { get; set; } = UserRole.User;

	[MaxLength(1024)]
	public string? AvatarUrl { get; set; }

	[Column(TypeName = "decimal(18,2)")]
	public decimal TotalCarbonSaved { get; set; }

	public int CurrentPoints { get; set; }

	[MaxLength(100)]
	public string? Region { get; set; }

	public bool IsActive { get; set; } = true;

	public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
	public ICollection<AiInsight> AiInsights { get; set; } = new List<AiInsight>();
	public ICollection<StepRecord> StepRecords { get; set; } = new List<StepRecord>();
}

