using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.Trip;

public class TripCalculateResponseDto
{
	public double DistanceKm { get; set; }
	public decimal EstimatedEmission { get; set; }
	public TransportMode TransportMode { get; set; }
}


