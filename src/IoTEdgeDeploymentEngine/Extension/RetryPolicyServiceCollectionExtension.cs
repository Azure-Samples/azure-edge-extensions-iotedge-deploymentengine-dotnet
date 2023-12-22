using System;
using System.Net.Http;
using Azure.Security.KeyVault.Secrets;
using IoTEdgeDeploymentEngine.Enums;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Retry;

namespace IoTEdgeDeploymentEngine.Extension
{
	/// <summary>
	/// Service collection extensions for retry policies and circuit breaker
	/// </summary>
	public static class RetryPolicyServiceCollectionExtension
	{
		/// <summary>
		/// Adds a retry policy with exponential backoff
		/// </summary>
		/// <param name="policyRegistry">Policy registry</param>
		/// <param name="retryCount">Retry count</param>
		/// <returns></returns>
		public static PolicyRegistry AddExponentialBackoffRetryPolicy(this PolicyRegistry policyRegistry,
			int retryCount = 10)
		{
			var policy = Policy
				.Handle<Microsoft.Azure.Devices.Common.Exceptions.ThrottlingException>()
				.Or<Microsoft.Azure.Devices.Common.Exceptions.IotHubThrottledException>()
				.WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
				onRetry:  (exception, retryAttempt, context) =>
				{
					context.GetLogger()?.LogWarning($"Polly request failed, retrying (error was '{exception.Message}')");
				});
			policyRegistry.Add(PolicyNames.ExponentialBackoffRetryPolicy.ToString(), policy);

			return policyRegistry;
		}

		/// <summary>
		/// Adds a infinite retry policy
		/// </summary>
		/// <param name="policyRegistry">Policy registry</param>
		/// <returns></returns>
		public static PolicyRegistry AddInfiniteRetryPolicy(this PolicyRegistry policyRegistry)
		{
			var policy = Policy
				.Handle<Exception>()
				.RetryForeverAsync();

			policyRegistry.Add(PolicyNames.InfiniteRetryPolicy.ToString(), policy);

			return policyRegistry;
		}

		/// <summary>
		/// Adds a circuit breaker policy
		/// </summary>
		/// <param name="policyRegistry">Policy registry</param>
		/// <param name="exceptionsBeforeBreaking">Exception count before circuit opens</param>
		/// <param name="durationOfBreakInSeconds">Duration in seconds before circuit resets</param>
		/// <returns></returns>
		public static PolicyRegistry AddCircuitBreakerPolicy(this PolicyRegistry policyRegistry,
			int exceptionsBeforeBreaking = 10, int durationOfBreakInSeconds = 60)
		{
			var circuitBreaker = Policy
				.Handle<Exception>()
				.CircuitBreakerAsync(exceptionsBeforeBreaking, TimeSpan.FromSeconds(durationOfBreakInSeconds));

			policyRegistry.Add(PolicyNames.CircuitBreakerPolicy.ToString(), circuitBreaker);

			return policyRegistry;
		}

	}
}