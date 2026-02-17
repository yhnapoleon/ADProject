using System.Security.Cryptography;
using System.Text;

namespace EcoLens.Api.Utilities;

public static class PasswordHasher
{
	public static string Hash(string plainText)
	{
		using var sha = SHA256.Create();
		var bytes = Encoding.UTF8.GetBytes(plainText);
		var hash = sha.ComputeHash(bytes);
		return Convert.ToHexString(hash);
	}
}

