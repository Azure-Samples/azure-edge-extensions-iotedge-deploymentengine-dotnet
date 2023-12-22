using System.Collections.Generic;
using System.Linq;
using IoTEdgeDeploymentEngine.Config;
using IoTEdgeDeploymentEngine.Extension;
using Microsoft.Extensions.Logging;

namespace IoTEdgeDeploymentEngine.Logic
{
	/// <inheritdoc />
	public class ModuleLogic : IModuleLogic
	{
		/// <summary>
		/// Logger instance
		/// </summary>
		protected readonly ILogger<ModuleLogic> _logger;

		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="logger">Logger</param>
		public ModuleLogic(ILogger<ModuleLogic> logger)
		{
			_logger = logger;
		}

		/// <inheritdoc />
		public virtual IEnumerable<DeploymentConfig> SelectDeployments(KeyValuePair<string, List<DeploymentConfig>> assignment)
		{
			_logger.LogInformation("SelectDeployments - Prioritization of deployments started.");
			
			if (!assignment.Value.Any(a => a.Category == DeploymentCategory.AutomaticDeployment))
			{
				_logger.LogWarning(
					$"ProcessDeviceAssignment - no Automatic (base) deployment found matching device '{assignment.Key}'. Skipping this assignment.");
				return Enumerable.Empty<DeploymentConfig>();
			}

			//https://learn.microsoft.com/en-us/azure/iot-edge/module-deployment-monitoring?view=iotedge-1.4#layered-deployment
			//layered deployments must have higher priority than the automatic deployment with highest priority
			var lowestPrio = assignment.Value.Where(a => a.Category == DeploymentCategory.AutomaticDeployment)
				.Max(a => a.Priority);
			var lastCreatedTimeStamp = assignment.Value
				.Where(a => a.Category == DeploymentCategory.AutomaticDeployment && a.Priority == lowestPrio)
				.OrderByDescending(a => a.ManifestConfig.CreatedTimeUtc)
				.FirstOrDefault()?.ManifestConfig
				.CreatedTimeUtc;
			var ordered = assignment.Value
				.Where(a =>
					(a.Priority >= lowestPrio && a.Category == DeploymentCategory.LayeredDeployment) ||
					(a.Priority >= lowestPrio && a.ManifestConfig.CreatedTimeUtc == lastCreatedTimeStamp &&
					 a.Category == DeploymentCategory.AutomaticDeployment))
				.OrderBy(a => a.Priority)
				.ThenBy(a => a.ManifestConfig.CreatedTimeUtc);
			
			_logger.LogInformation("SelectDeployments - Successfully prioritized of deployments.");
			return ordered;
		}
		
		/// <summary>
		/// Gets the edgeAgents modules specification from ModulesContent
		/// </summary>
		/// <param name="deploymentConfigs">Set of deployments for single device</param>
		/// <returns></returns>
		public virtual IEnumerable<KeyValuePair<string, object>> GetEdgeAgentModules(
			IEnumerable<DeploymentConfig> deploymentConfigs)
		{
			_logger.LogInformation("GetEdgeAgentModules - Selecting EdgeAgent modules from manifest.");
			
			return deploymentConfigs
				.Where(c => c.ManifestConfig.Content.ModulesContent.ContainsKey("$edgeAgent"))
				.OrderByDescending(c => c.Priority)
				.ThenByDescending(c => c.ManifestConfig.CreatedTimeUtc)
				.SelectMany(c => c.ManifestConfig.Content.ModulesContent["$edgeAgent"])
				.FirstPerKey();
		}

		/// <inheritdoc />
		public virtual IEnumerable<KeyValuePair<string, object>> GetEdgeHubProps(
			IEnumerable<DeploymentConfig> deploymentConfigs)
		{
			_logger.LogInformation("GetEdgeHubProps - Selecting EdgeHub properties from manifest.");
			
			return deploymentConfigs
				.Where(c => c.ManifestConfig.Content.ModulesContent.ContainsKey("$edgeHub"))
				.OrderByDescending(c => c.Priority)
				.ThenByDescending(c => c.ManifestConfig.CreatedTimeUtc)
				.SelectMany(c => c.ManifestConfig.Content.ModulesContent["$edgeHub"])
				.FirstPerKey();
		}
		
		/// <inheritdoc />
		public virtual IEnumerable<KeyValuePair<string, IDictionary<string, object>>> GetOuterModules(
			IEnumerable<DeploymentConfig> deploymentConfigs)
		{
			_logger.LogInformation("GetEdgeHubProps - Selecting modules from manifest.");
			
			return deploymentConfigs
				.OrderByDescending(c => c.Priority)
				.ThenByDescending(c => c.ManifestConfig.CreatedTimeUtc)
				.SelectMany(c =>
					c.ManifestConfig.Content.ModulesContent.Where(m => m.Key != "$edgeAgent" && m.Key != "$edgeHub"))
				.FirstPerKey();
		}
	}
}