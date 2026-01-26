using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public ProductController(ApplicationDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// 通过条形码查询对应的商品碳因子。
	/// </summary>
	[HttpGet("{barcode}")]
	public async Task<ActionResult<ProductLookupResponseDto>> GetByBarcode([FromRoute] string barcode, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(barcode))
		{
			return BadRequest("Barcode is required.");
		}

		// 使用 BarcodeReference 表查询，而不是 CarbonReference.Barcode
		var barcodeRef = await _db.BarcodeReferences.AsNoTracking()
			.Include(b => b.CarbonReference)
			.FirstOrDefaultAsync(b => b.Barcode == barcode, ct);

		if (barcodeRef is null || barcodeRef.CarbonReference is null)
		{
			return NotFound();
		}

		var carbonRef = barcodeRef.CarbonReference;
		return Ok(new ProductLookupResponseDto
		{
			Name = carbonRef.LabelName,
			Co2Factor = carbonRef.Co2Factor,
			Unit = carbonRef.Unit,
			Barcode = barcodeRef.Barcode
		});
	}
}


