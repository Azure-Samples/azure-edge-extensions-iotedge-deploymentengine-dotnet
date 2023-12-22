using System.Threading.Tasks;
using IoTEdgeDeploymentEngine.Config;

namespace IoTEdgeDeploymentEngine
{
	/// <summary>
	/// Interface as an entry point for the IoTEdgeDeploymentEngine.
	/// </summary>
	public interface IIoTEdgeDeploymentBuilder
	{
		/// <summary>
		/// Applies deployments for a subset of defined devices per query.
		/// </summary>
		/// <returns></returns>
		Task ApplyDeployments();

		/// <summary>
		/// Adds a deployment to the file system.
		/// </summary>
		/// <param name="filePath">Deployment full file path</param>
		/// <param name="fileContent">Deployment manifest JSON string</param>
		/// <param name="category">Deployment category (automatic, layered)</param>
		/// <returns></returns>
		Task AddDeployment(string filePath, string fileContent, DeploymentCategory category);

		/// <summary>
		/// Gets content of a single file.
		/// </summary>
		/// <param name="filePath">File path.</param>
		/// <param name="category">Deployment category (automatic, layered)</param>
		/// <returns></returns>
		Task<dynamic> GetFileContent(string filePath, DeploymentCategory category);
	}
}