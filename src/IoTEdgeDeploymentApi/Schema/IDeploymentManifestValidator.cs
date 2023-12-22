namespace IoTEdgeDeploymentApi.Schema;

/// <summary>
/// Validates DeploymentManifest schema
/// </summary>
public interface IDeploymentManifestValidator
{
	/// <summary>
	/// Validates the json
	/// </summary>
	/// <param name="json">JSON string</param>
	/// <param name="message">Validation message</param>
	/// <returns></returns>
	bool Validate(string json, out string message);
}