using System;
using System.Threading.Tasks;
using IoTEdgeDeploymentEngine;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace IoTEdgeDeploymentApi
{
	/// <summary>
	/// Scheduler function to apply deployments.
	/// </summary>
	public class DeploymentScheduler
	{
		private readonly IIoTEdgeDeploymentBuilder _ioTEdgeDeploymentBuilder;
		private readonly ILogger<DeploymentScheduler> _logger;

		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="ioTEdgeDeploymentBuilder">IoTEdgeDeploymentBuilder instance per DI</param>
		/// <param name="logger">ILogger instance per DI</param>
		public DeploymentScheduler(IIoTEdgeDeploymentBuilder ioTEdgeDeploymentBuilder,
			ILogger<DeploymentScheduler> logger)
		{
			_ioTEdgeDeploymentBuilder = ioTEdgeDeploymentBuilder;
			_logger = logger;
		}

		/// <summary>
		/// Timer based function that executes deployments every day at 12:00am
		/// </summary>
		/// <param name="myTimer">Timer object</param>
		[FunctionName("DeploymentScheduler")]
		public async Task Run([TimerTrigger("0 0 12 * * *", RunOnStartup = false)] TimerInfo myTimer)
		{
			_logger.LogInformation($"DeploymentScheduler start at {DateTime.UtcNow}");
			await _ioTEdgeDeploymentBuilder.ApplyDeployments();
			_logger.LogInformation($"DeploymentScheduler finished at {DateTime.UtcNow}");
		}
	}
}