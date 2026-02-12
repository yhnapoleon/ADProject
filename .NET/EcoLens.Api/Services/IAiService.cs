using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.Services
{
	public interface IAiService
	{
		Task<string> GetAnswerAsync(string userPrompt);

		/// <summary>
		/// 带 System Prompt 的对话接口（用于 RAG/Guardrail 等场景）。
		/// </summary>
		Task<string> GetAnswerAsync(string userPrompt, string? systemPrompt, CancellationToken ct = default);
		Task<string> AnalyzeImageAsync(string prompt, IFormFile image);
	}
}

