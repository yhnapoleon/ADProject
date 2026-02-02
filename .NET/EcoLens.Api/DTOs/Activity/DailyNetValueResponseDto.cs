namespace EcoLens.Api.DTOs.Activity;

public class DailyNetValueResponseDto
{
	public decimal Value { get; set; }
	public bool IsQualified { get; set; }
	public DailyNetValueBreakdownDto Breakdown { get; set; } = new DailyNetValueBreakdownDto();
}

public class DailyNetValueBreakdownDto
{
	public decimal StepSaving { get; set; }
	public decimal Benchmark { get; set; }
	public decimal Emission { get; set; }
}


