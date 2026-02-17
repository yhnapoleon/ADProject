namespace EcoLens.Api.DTOs.Product;

public class ProductLookupResponseDto
{
	public string Name { get; set; } = string.Empty;
	public decimal Co2Factor { get; set; }
	public string Unit { get; set; } = string.Empty;
	public string? Barcode { get; set; }
}


