namespace EcoLens.Api.DTOs.Auth;

public class AuthResponseDto
{
	public string Token { get; set; } = string.Empty;
	// 为前端兼容提供别名字段
	public string AccessToken
	{
		get => Token;
		set => Token = value;
	}
	public UserSummaryDto User { get; set; } = new();
}

