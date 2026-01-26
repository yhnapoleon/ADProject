using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.Auth;

public class UserSummaryDto
{
	public int Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string Nickname { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public UserRole Role { get; set; } = UserRole.User;
	public string? AvatarUrl { get; set; }
	public decimal TotalCarbonSaved { get; set; }
	public int CurrentPoints { get; set; }
}

