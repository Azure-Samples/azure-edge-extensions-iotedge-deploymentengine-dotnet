using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace IoTEdgeDeploymentApi.Security;

/// <inheritdoc />
public class JwtValidator : IJwtValidator
{
	/// <inheritdoc />
	public ClaimsPrincipal Validate(string jwtToken)
	{
		var token = jwtToken.Split(" ");
		if (token.Length != 2)
			return null;

		var value = new AuthenticationHeaderValue(token[0], token[1]);

		if (value?.Scheme != "Bearer")
			return null;

		var validationParameter = new TokenValidationParameters
		{
			RequireSignedTokens = false,
			ValidAudience = Environment.GetEnvironmentVariable("OpenApi__Auth__Audience"),
			ValidateAudience = true,
			ValidateIssuer = false,
			ValidateIssuerSigningKey = false,
			ValidateLifetime = true,
			SignatureValidator = (t, param) => new JwtSecurityToken(t),
		};

		ClaimsPrincipal result = null;
		while (result == null)
		{
			var handler = new JwtSecurityTokenHandler();
			result = handler.ValidateToken(value.Parameter, validationParameter, out var tokenValue);
		}

		return result;
	}
}