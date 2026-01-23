using System.ComponentModel.DataAnnotations;

namespace EcoLens.Api.DTOs.Barcode
{
    public class UpdateBarcodeReferenceDto
    {
        [Required]
        public int Id { get; set; }

        [MaxLength(200)]
        public string? ProductName { get; set; }

        public int? CarbonReferenceId { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }
    }
}

