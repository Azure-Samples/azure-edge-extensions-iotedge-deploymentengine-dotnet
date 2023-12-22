namespace IoTEdgeDeploymentEngine.Config
{
	/// <summary>
	/// Manifest directory configuration object
	/// </summary>
	public interface IManifestConfig
	{
		/// <summary>
		/// Directory root folder for automatic deployment manifest files
		/// </summary>
		public string DirectoryRootAutomatic { get; set; }

		/// <summary>
		/// Directory root folder for layered deployment manifest files
		/// </summary>
		public string DirectoryRootLayered { get; set; }
	}
}