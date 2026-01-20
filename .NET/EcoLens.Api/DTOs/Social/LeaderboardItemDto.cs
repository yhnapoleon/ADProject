namespace EcoLens.Api.DTOs.Social;

public class LeaderboardItemDto
{
	public string Username { get; set; } = string.Empty;
	public string? AvatarUrl { get; set; }
	public decimal TotalCarbonSaved { get; set; }
}

