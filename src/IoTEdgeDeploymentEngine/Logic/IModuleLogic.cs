using System.Collections.Generic;
using IoTEdgeDeploymentEngine.Config;

namespace IoTEdgeDeploymentEngine.Logic
{
	/// <summary>
	/// Extracts the modules from manifest file based on common logic
	/// referring to https://learn.microsoft.com/en-us/azure/iot-edge/module-deployment-monitoring?view=iotedge-1.4#layered-deployment
	/// </summary>
	public interface IModuleLogic
	{
		/// <summary>
		/// Selects and assesses deployments per device and determines if relevant
		/// </summary>
		/// <param name="assignment"></param>
		/// <returns></returns>
		IEnumerable<DeploymentConfig> SelectDeployments(KeyValuePair<string, List<DeploymentConfig>> assignment);
		
		/// <summary>
		/// Gets the edgeAgents modules specification from ModulesContent
		/// </summary>
		/// <param name="deploymentConfigs">Set of deployments for single device</param>
		/// <returns></returns>
		IEnumerable<KeyValuePair<string, object>> GetEdgeAgentModules(
			IEnumerable<DeploymentConfig> deploymentConfigs);
		
		/// <summary>
		/// Gets the edgeHub properties specification from ModulesContent
		/// </summary>
		/// <param name="deploymentConfigs">Set of deployments for single device</param>
		/// <returns></returns>
		IEnumerable<KeyValuePair<string, object>> GetEdgeHubProps(
			IEnumerable<DeploymentConfig> deploymentConfigs);
		
		/// <summary>
		/// Gets the edgeAgents modules specification from ModulesContent
		/// </summary>
		/// <param name="deploymentConfigs">Set of deployments for single device</param>
		/// <returns></returns>
		IEnumerable<KeyValuePair<string, IDictionary<string, object>>> GetOuterModules(
			IEnumerable<DeploymentConfig> deploymentConfigs);
	}
}