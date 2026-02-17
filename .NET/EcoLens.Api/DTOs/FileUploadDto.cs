using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.DTOs;

public class FileUploadDto
{
	[Required]
	public IFormFile File { get; set; } = null!;
}

