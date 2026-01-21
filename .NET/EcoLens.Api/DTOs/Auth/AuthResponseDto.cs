namespace EcoLens.Api.DTOs.Auth;

public class AuthResponseDto
{
	public string Token { get; set; } = string.Empty;
	public UserSummaryDto User { get; set; } = new();
}

