using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EcoLens.Api.Services;
using EcoLens.Api.Utilities;
using Microsoft.Extensions.Options;
using Xunit;

namespace EcoLens.Tests.Services;

public class AuthServiceTests
{
	private static IOptions<JwtOptions> CreateJwtOptions()
	{
		return Options.Create(new JwtOptions
		{
			Issuer = "test-issuer",
			Audience = "test-audience",
			Key = "this-is-a-test-key-at-least-32-chars-long!!",
			ExpirationMinutes = 60
		});
	}

	[Fact]
	public async Task GenerateTokenAsync_ReturnsValidJwt_WithoutCustomClaims()
	{
		var options = CreateJwtOptions();
		var sut = new AuthService(options);
		var token = await sut.GenerateTokenAsync("user-123", null);
		Assert.NotNull(token);
		var handler = new JwtSecurityTokenHandler();
		var jwt = handler.ReadJwtToken(token);
		Assert.Equal("user-123", jwt.Subject);
		Assert.Equal("test-issuer", jwt.Issuer);
		Assert.Contains(jwt.Audiences, a => a == "test-audience");
		Assert.True(jwt.ValidTo > DateTime.UtcNow);
	}

	[Fact]
	public async Task GenerateTokenAsync_IncludesCustomClaims_WhenProvided()
	{
		var options = CreateJwtOptions();
		var sut = new AuthService(options);
		var customClaims = new Dictionary<string, string>
		{
			{ "role", "admin" },
			{ "region", "SG" }
		};
		var token = await sut.GenerateTokenAsync("user-456", customClaims);
		Assert.NotNull(token);
		var handler = new JwtSecurityTokenHandler();
		var jwt = handler.ReadJwtToken(token);
		var role = jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
		var region = jwt.Claims.FirstOrDefault(c => c.Type == "region")?.Value;
		Assert.Equal("admin", role);
		Assert.Equal("SG", region);
	}
}
