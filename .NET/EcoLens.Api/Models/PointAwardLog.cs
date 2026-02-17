using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

/// <summary>
/// Point award log for tracking today/monthly points (leaderboard, etc.).
/// One record is written each time points are awarded. AwardedAt indicates the date the points belong to (used for daily/monthly aggregation).
/// </summary>
[Table("PointAwardLogs")]
public class PointAwardLog
{
	public int Id { get; set; }
	public int UserId { get; set; }
	/// <summary>Points awarded in this record</summary>
	public int Points { get; set; }
	/// <summary>Date the points belong to (UTC, used for today/monthly aggregation)</summary>
	public DateTime AwardedAt { get; set; }
	/// <summary>Source: DailyNeutral, WeeklyBonus, TreePlanting, Step</summary>
	[MaxLength(32)]
	public string Source { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
