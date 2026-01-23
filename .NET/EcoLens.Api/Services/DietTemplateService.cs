using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Diet;
using EcoLens.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Services;

public class DietTemplateService : IDietTemplateService
{
	private readonly ApplicationDbContext _db;

	public DietTemplateService(ApplicationDbContext db)
	{
		_db = db;
	}

	public async Task<DietTemplateDto> CreateTemplateAsync(Guid userId, CreateDietTemplateRequest request, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(request.TemplateName))
		{
			throw new ArgumentException("TemplateName is required.", nameof(request.TemplateName));
		}

		var template = new DietTemplate
		{
			UserId = userId,
			TemplateName = request.TemplateName.Trim()
		};

		if (request.Items is { Count: > 0 })
		{
			foreach (var item in request.Items)
			{
				template.Items.Add(new DietTemplateItem
				{
					FoodId = item.FoodId,
					Quantity = item.Quantity,
					Unit = item.Unit
				});
			}
		}

		await _db.DietTemplates.AddAsync(template, cancellationToken);
		await _db.SaveChangesAsync(cancellationToken);

		// 重新查询以包含 Items 与时间戳
		var created = await _db.DietTemplates
			.AsNoTracking()
			.Include(t => t.Items)
			.FirstAsync(t => t.Id == template.Id, cancellationToken);

		return MapToDto(created);
	}

	public async Task<List<DietTemplateDto>> GetUserTemplatesAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		var list = await _db.DietTemplates
			.AsNoTracking()
			.Include(t => t.Items)
			.Where(t => t.UserId == userId)
			.OrderByDescending(t => t.CreatedAt)
			.ToListAsync(cancellationToken);

		return list.Select(MapToDto).ToList();
	}

	private static DietTemplateDto MapToDto(DietTemplate template)
	{
		return new DietTemplateDto
		{
			Id = template.Id,
			UserId = template.UserId,
			TemplateName = template.TemplateName,
			CreatedAt = template.CreatedAt,
			Items = template.Items.Select(i => new DietTemplateItemDto
			{
				Id = i.Id,
				FoodId = i.FoodId,
				Quantity = i.Quantity,
				Unit = i.Unit
			}).ToList()
		};
	}
}

