using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.Admin;

public class CarbonReferenceDto
{
	public int Id { get; set; }
	public string LabelName { get; set; } = string.Empty;
	public CarbonCategory Category { get; set; }
	public decimal Co2Factor { get; set; }
	public string Unit { get; set; } = string.Empty;
	public string? Region { get; set; }
}



