using System.ComponentModel.DataAnnotations;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.Trip;

public class TripCalculateRequestDto
{
	[Required]
	[MaxLength(256)]
	public string StartLocation { get; set; } = string.Empty;

	[Required]
	[MaxLength(256)]
	public string EndLocation { get; set; } = string.Empty;

	[Required]
	public TransportMode TransportMode { get; set; }
}


