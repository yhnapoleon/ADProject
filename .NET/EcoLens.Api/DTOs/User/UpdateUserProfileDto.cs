namespace EcoLens.Api.DTOs.User;

public class UpdateUserProfileDto
{
	public string? Nickname { get; set; } // 映射到 ApplicationUser.Username
	public string? Avatar { get; set; }   // AvatarUrl
	public string? Location { get; set; } // Region
	public string? Email { get; set; }
	public string? BirthDate { get; set; } // yyyy-MM-dd
}





