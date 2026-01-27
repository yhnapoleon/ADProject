using EcoLens.Api.Models.Enums;
using System.Text.Json.Serialization;

namespace EcoLens.Api.DTOs.Auth;

public class UserSummaryDto
{
	public string Id { get; set; } = string.Empty;
	public string Username { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Nickname { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string? Location { get; set; }
	public string? BirthDate { get; set; }
	public UserRole Role { get; set; } = UserRole.User;
	public string? Avatar { get; set; }
	public string? AvatarUrl { get; set; }
	public decimal TotalCarbonSaved { get; set; }
	public int CurrentPoints { get; set; }
	public int PointsWeek { get; set; }
	public int PointsMonth { get; set; }
	public int PointsTotal { get; set; }
	public int JoinDays { get; set; }
}

