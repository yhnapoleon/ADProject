namespace EcoLens.Api.DTOs.Barcode
{
    public class BarcodeReferenceResponseDto
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int? CarbonReferenceId { get; set; }
        public string? CarbonReferenceLabel { get; set; }
        public decimal? Co2Factor { get; set; }
        public string? Unit { get; set; }
        public string? Category { get; set; }
        public string? Brand { get; set; }
    }
}

