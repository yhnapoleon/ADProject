using System.Collections.Generic;

namespace EcoLens.Api.DTOs.Activity;

public class DashboardDto
{
	public List<decimal> WeeklyTrend { get; set; } = new();
	public decimal NeutralityGap { get; set; }
	public decimal TreesPlanted { get; set; }
}



