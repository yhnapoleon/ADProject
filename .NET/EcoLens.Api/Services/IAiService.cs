using System.Threading.Tasks;

namespace EcoLens.Api.Services
{
	public interface IAiService
	{
		Task<string> GetAnswerAsync(string userPrompt);
	}
}

