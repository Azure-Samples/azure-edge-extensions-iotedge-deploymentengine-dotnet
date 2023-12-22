namespace IoTEdgeDeploymentApi.Model
{
	/// <summary>
	/// Specifies a file name and content for the deployment manifest.
	/// </summary>
	public class DeploymentFile
	{
		/// <summary>
		/// File name and extension.
		/// </summary>
		public string FileName { get; set; }

		/// <summary>
		/// File Content of the deployment manifest in JSON string format.
		/// </summary>
		public string FileContent { get; set; }
	}
}

