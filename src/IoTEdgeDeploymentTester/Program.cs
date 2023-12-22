using IoTEdgeDeploymentEngine;
using IoTEdgeDeploymentEngine.Accessor;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using dotenv.net;
using IoTEdgeDeploymentEngine.Config;
using dotenv.net.Utilities;
using Azure.Identity;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using IoTEdgeDeploymentEngine.Extension;
using IoTEdgeDeploymentEngine.Logic;
using IoTEdgeDeploymentEngine.Util;
using Microsoft.Extensions.Azure;
using Polly;
using Polly.Registry;

DotEnv.Load();
var host = ConfigureServices(args);
var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("IoT Edge Deployment Engine - Run started.");

EnvReader.TryGetBooleanValue("CONTINUE_ON_ERROR", out var continueOnError);
try
{
	var serviceDeployment = host.Services.GetRequiredService<IIoTEdgeDeploymentBuilder>();
	await serviceDeployment.ApplyDeployments();
	
	logger.LogInformation("IoT Edge Deployment Engine - Run completed.");
}
catch (Exception ex)
{
	logger.LogError(
		$"IoT Edge Deployment Engine resulted with errors. Please check the previous log of the run - {ex.Message}");
	if (!continueOnError)
		throw;
}

return;

IHost ConfigureServices(string[] args)
{
	var iotHubHostName = EnvReader.GetStringValue("IOTHUB_HOSTNAME");
	EnvReader.TryGetStringValue("KEYVAULT_URI", out var keyVaultUri);
	var rootDirectory = EnvReader.GetStringValue("ROOT_MANIFESTS_FOLDER");
	var rootDirectoryAutomatic = Path.Combine(rootDirectory, "AutomaticDeployment");
	var rootDirectoryLayered = Path.Combine(rootDirectory, "LayeredDeployment");

	TokenCredential tokenCredential = new DefaultAzureCredential();
	return Host.CreateDefaultBuilder(args)
		.ConfigureServices((_, services) =>
			services
				.AddSingleton<RegistryManager>(_ => RegistryManager.Create(iotHubHostName, tokenCredential))
				.AddSingleton<IKeyVaultAccessor>(_ =>
				{
					var logger = services.BuildServiceProvider().GetRequiredService<ILogger<KeyVaultAccessor>>();
					if (string.IsNullOrEmpty(keyVaultUri))
						return new KeyVaultAccessor(null, logger);
					
					return new KeyVaultAccessor(new SecretClient(
							new Uri(keyVaultUri),
							tokenCredential,
							new SecretClientOptions()
							{
								Retry =
								{
									MaxRetries = 3,
									Delay = TimeSpan.FromSeconds(5),
									MaxDelay = TimeSpan.FromSeconds(15),
									Mode = RetryMode.Exponential,
									NetworkTimeout = TimeSpan.FromSeconds(60)
								}
							}),
						logger);
				})
				.AddSingleton<IIoTEdgeDeploymentBuilder, IoTEdgeDeploymentBuilder>()
				.AddSingleton<IPolicyRegistry<string>>(_ =>
				{
					var policyRegistry = new PolicyRegistry();
					policyRegistry
						.AddExponentialBackoffRetryPolicy()
						.AddInfiniteRetryPolicy()
						.AddCircuitBreakerPolicy();
					return policyRegistry;
				})
				.AddSingleton<IIoTEdgeDeploymentBuilder, IoTEdgeDeploymentBuilder>()
				.AddSingleton<IIoTHubAccessor, IoTHubAccessor>()
				.AddSingleton<IModuleLogic, ModuleLogic>()
				.AddSingleton<IManifestConfig>(c => new ManifestConfig
				{
					DirectoryRootAutomatic = rootDirectoryAutomatic,
					DirectoryRootLayered = rootDirectoryLayered
				})
				.AddLogging())
		.Build();
}