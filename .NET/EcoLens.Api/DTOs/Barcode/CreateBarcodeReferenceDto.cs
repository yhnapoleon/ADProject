using System.ComponentModel.DataAnnotations;

namespace EcoLens.Api.DTOs.Barcode
{
    public class CreateBarcodeReferenceDto
    {
        [Required]
        [MaxLength(50)]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        public int? CarbonReferenceId { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }
    }
}

