using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.DTOs.Diet;

namespace EcoLens.Api.Services;

public interface IDietTemplateService
{
	Task<DietTemplateDto> CreateTemplateAsync(Guid userId, CreateDietTemplateRequest request, CancellationToken cancellationToken = default);
	Task<List<DietTemplateDto>> GetUserTemplatesAsync(Guid userId, CancellationToken cancellationToken = default);
}

