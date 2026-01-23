using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.DTOs;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.Services;

public interface IVisionService
{
	Task<VisionPredictionResponseDto> PredictAsync(IFormFile image, CancellationToken cancellationToken);
}


