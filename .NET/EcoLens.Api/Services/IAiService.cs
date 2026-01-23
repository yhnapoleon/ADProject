using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.Services
{
	public interface IAiService
	{
		Task<string> GetAnswerAsync(string userPrompt);
		Task<string> AnalyzeImageAsync(string prompt, IFormFile image);
	}
}

