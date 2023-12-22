using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoTEdgeDeploymentEngine.Enums;
using IoTEdgeDeploymentEngine.Extension;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace IoTEdgeDeploymentEngine.Accessor
{
	/// <inheritdoc />
	public class IoTHubAccessor : IIoTHubAccessor
	{
		private readonly RegistryManager _registryManager;
		private readonly IAsyncPolicy _retryPolicy;
		private readonly ILogger<IoTHubAccessor> _logger;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="registryManager">RegistryManager instance</param>
        /// <param name="policyRegistry">Retry policy</param>
		/// <param name="logger">Logger</param>
        public IoTHubAccessor(RegistryManager registryManager, IPolicyRegistry<string> policyRegistry, ILogger<IoTHubAccessor> logger)
		{
			_registryManager = registryManager;
			_logger = logger;
			
			_retryPolicy = policyRegistry.Get<IAsyncPolicy>(PolicyNames.ExponentialBackoffRetryPolicy.ToString());
			var circuitBreakerPolicy = policyRegistry.Get<IAsyncPolicy>(PolicyNames.CircuitBreakerPolicy.ToString());
			_retryPolicy.WrapAsync(circuitBreakerPolicy);

		}

		/// <inheritdoc />
		public async Task<IEnumerable<string>> GetDeviceIdsByCondition(string targetCondition)
		{
			_logger.LogInformation(
				$"GetDeviceIdsByCondition - Retrieving devices from IoTHub for query condition: {targetCondition}.");
			
			var deviceIds = new List<string>();

			var sql = $"Select * from devices where {targetCondition}";
            var context = new Polly.Context().WithLogger<ILogger>(_logger);

            await _retryPolicy.ExecuteAsync(async action => 
			{
				var query = _registryManager.CreateQuery(sql);

				while (query.HasMoreResults)
				{
					var twin = await query.GetNextAsTwinAsync();
					deviceIds.AddRange(twin.Select(f => f.DeviceId));
				}
			},
			context);

            _logger.LogInformation(
	            $"GetDeviceIdsByCondition - Successfully retrieved devices from IoTHub for query condition: {targetCondition}.");
            return deviceIds;
		}

		/// <inheritdoc />
		public async Task ApplyDeploymentPerDevice(string deviceId, ConfigurationContent configurationContent)
		{
			_logger.LogInformation(
				$"ProcessDeviceAssignment - Applying configuration for deviceId: {deviceId}");
			
            var context = new Polly.Context().WithLogger<ILogger>(_logger);
            await _retryPolicy.ExecuteAsync(async action =>
			{
				await _registryManager.ApplyConfigurationContentOnDeviceAsync(deviceId, configurationContent);
			}, context);
            
            _logger.LogInformation(
	            $"ProcessDeviceAssignment - Successfully applied configuration for deviceId: {deviceId}");
		}
	}
}