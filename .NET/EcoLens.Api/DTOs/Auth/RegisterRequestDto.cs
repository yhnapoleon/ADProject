using System.ComponentModel.DataAnnotations;

namespace EcoLens.Api.DTOs.Auth;

public class RegisterRequestDto
{
	[Required]
	[MaxLength(100)]
	public string Username { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	[MaxLength(256)]
	public string Email { get; set; } = string.Empty;

	[Required]
	[MaxLength(128)]
	public string Password { get; set; } = string.Empty;
}

