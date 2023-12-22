using System.Collections.Generic;
using System.Linq;
using IoTEdgeDeploymentEngine.Config;
using Microsoft.Extensions.Logging;

namespace IoTEdgeDeploymentEngine.Logic
{
	/// <inheritdoc />
	public class SampleModuleLogic : ModuleLogic
	{
		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="logger">Logger</param>
		public SampleModuleLogic(ILogger<SampleModuleLogic> logger) : base(logger)
		{
		}

		/// <inheritdoc />
		public override IEnumerable<DeploymentConfig> SelectDeployments(KeyValuePair<string, List<DeploymentConfig>> assignment)
		{
			_logger.LogInformation("SelectDeployments - Prioritization of deployments started.");
			
			//https://learn.microsoft.com/en-us/azure/iot-edge/module-deployment-monitoring?view=iotedge-1.4#layered-deployment
			//layered deployments must have higher priority than the automatic deployment with highest priority
			return assignment.Value
				.Where(a => a.Category == DeploymentCategory.LayeredDeployment)
				.OrderByDescending(c => c.Priority)
				.ThenByDescending(c => c.ManifestConfig.CreatedTimeUtc);
		}
	}
}