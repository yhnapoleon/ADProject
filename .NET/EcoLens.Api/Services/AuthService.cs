using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EcoLens.Api.Utilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EcoLens.Api.Services;

public class AuthService : IAuthService
{
	private readonly JwtOptions _jwtOptions;

	public AuthService(IOptions<JwtOptions> jwtOptions)
	{
		_jwtOptions = jwtOptions.Value;
	}

	public Task<string> GenerateTokenAsync(string subject, IDictionary<string, string>? customClaims = null)
	{
		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, subject),
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
		};

		if (customClaims != null)
		{
			foreach (var kvp in customClaims)
			{
				claims.Add(new Claim(kvp.Key, kvp.Value));
			}
		}

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _jwtOptions.Issuer,
			audience: _jwtOptions.Audience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
			signingCredentials: creds
		);

		var jwt = new JwtSecurityTokenHandler().WriteToken(token);
		return Task.FromResult(jwt);
	}
}

