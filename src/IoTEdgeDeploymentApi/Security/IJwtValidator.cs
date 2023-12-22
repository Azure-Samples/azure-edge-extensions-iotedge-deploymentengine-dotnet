using System.Security.Claims;

namespace IoTEdgeDeploymentApi.Security;

/// <summary>
/// Validates security token
/// </summary>
public interface IJwtValidator
{
	/// <summary>
	/// Validates the Bearer token
	/// </summary>
	/// <param name="jwtToken"></param>
	/// <returns></returns>
	ClaimsPrincipal Validate(string jwtToken);
}