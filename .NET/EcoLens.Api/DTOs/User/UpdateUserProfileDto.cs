namespace EcoLens.Api.DTOs.User;

public class UpdateUserProfileDto
{
	public string? Nickname { get; set; } // 映射到 ApplicationUser.Username
	public string? AvatarUrl { get; set; }
	public string? Region { get; set; }
}





