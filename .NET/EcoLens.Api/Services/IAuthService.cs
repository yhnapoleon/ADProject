using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoLens.Api.Services;

public interface IAuthService
{
	Task<string> GenerateTokenAsync(string subject, IDictionary<string, string>? customClaims = null);
}

