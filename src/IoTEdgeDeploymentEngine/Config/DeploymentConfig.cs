using Microsoft.Azure.Devices;

namespace IoTEdgeDeploymentEngine.Config
{
	/// <summary>
	/// Deployment configuration
	/// </summary>
	public class DeploymentConfig
	{
		/// <summary>
		/// Unique manifest file name to find the right configuration to merge
		/// </summary>
		public string ManifestFile { get; set; }
        
		/// <summary>
		/// Priority of the deployment
		/// </summary>
		public int Priority { get; set; }
        
		/// <summary>
		/// Automatic or layered deployment
		/// </summary>
		public DeploymentCategory Category { get; set; }

		/// <summary>
		/// Configuration of manifest file
		/// </summary>
		public Configuration ManifestConfig { get; set; }
	}
}