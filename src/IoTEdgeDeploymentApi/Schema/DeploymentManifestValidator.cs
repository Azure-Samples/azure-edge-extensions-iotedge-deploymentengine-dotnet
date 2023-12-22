using System.Text.Json.Nodes;
using Json.Schema;
using JsonSchema = Json.Schema.JsonSchema;


namespace IoTEdgeDeploymentApi.Schema;

/// <inheritdoc />
public class DeploymentManifestValidator : IDeploymentManifestValidator
{
	/// <inheritdoc />
	public bool Validate(string json, out string message)
	{
		var instance = JsonNode.Parse(json);
		
		var schema = JsonSchema.FromFile(@"./Schema/DeploymentManifestSchema.json");
		
		var options = new ValidationOptions() { OutputFormat = OutputFormat.Detailed };
		var results = schema.Validate(instance, options);
		message = results.Message;
		
		return results.IsValid;
	}
	
	
}