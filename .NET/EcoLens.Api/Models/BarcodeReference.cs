using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("BarcodeReferences")]
public class BarcodeReference : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Barcode { get; set; } = string.Empty; // 存储条形码，应设置为唯一索引

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public int? CarbonReferenceId { get; set; } // 关联到 CarbonReference 表

    [ForeignKey(nameof(CarbonReferenceId))]
    public CarbonReference? CarbonReference { get; set; }

    // 可以添加其他产品相关信息，例如 Category, Brand 等
    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }
}

