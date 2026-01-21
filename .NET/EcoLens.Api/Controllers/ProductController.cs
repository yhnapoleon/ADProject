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

		var entity = await _db.CarbonReferences.AsNoTracking()
			.FirstOrDefaultAsync(c => c.Barcode == barcode, ct);

		if (entity is null)
		{
			return NotFound();
		}

		return Ok(new ProductLookupResponseDto
		{
			Name = entity.LabelName,
			Co2Factor = entity.Co2Factor,
			Unit = entity.Unit,
			Barcode = entity.Barcode
		});
	}
}


