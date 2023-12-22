using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IoTEdgeDeploymentEngine.Accessor;
using IoTEdgeDeploymentEngine.Config;
using IoTEdgeDeploymentEngine.Extension;
using IoTEdgeDeploymentEngine.Logic;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IoTEdgeDeploymentEngine
{
	/// <summary>
	/// Implementation for the deployment engine providing common functionalities
	/// </summary>
	public class IoTEdgeDeploymentBuilder : IIoTEdgeDeploymentBuilder
	{
		private readonly IIoTHubAccessor _ioTHubAccessor;
		private readonly IKeyVaultAccessor _keyVaultAccessor;
		private readonly IManifestConfig _manifestConfig;
		private readonly IModuleLogic _moduleLogic;
		private readonly ILogger<IoTEdgeDeploymentBuilder> _logger;

		private const string SecretNameRegEx = "{{(?<secretName>[^}]*)";

		/// <summary>
		/// ctor
		/// </summary>
		public IoTEdgeDeploymentBuilder(IIoTHubAccessor ioTHubAccessor, IKeyVaultAccessor keyVaultAccessor,
			IManifestConfig manifestConfig, IModuleLogic moduleLogic, ILogger<IoTEdgeDeploymentBuilder> logger)
		{
			_ioTHubAccessor = ioTHubAccessor;
			_keyVaultAccessor = keyVaultAccessor;
			_manifestConfig = manifestConfig;
			_moduleLogic = moduleLogic;
			_logger = logger;
		}

		/// <inheritdoc />
		public async Task ApplyDeployments()
		{
			var files = await ReadAllFiles(_manifestConfig.DirectoryRootAutomatic,
				_manifestConfig.DirectoryRootLayered);
			_logger.LogDebug($"ApplyDeployments - total files in both directories: {files.Count}.");

			var assignments = await CreateDeviceDeploymentAssignments(files);

			var tasks = assignments.Select(ProcessDeviceAssignment).ToList();

			await Task.WhenAll(tasks);
		}

		private async Task ProcessDeviceAssignment(KeyValuePair<string, List<DeploymentConfig>> assignment)
		{
			_logger.LogInformation($"ProcessDeviceAssignment - Process deployments for deviceId: {assignment.Key}.");

			var deployments = _moduleLogic.SelectDeployments(assignment).ToArray();
			if (!deployments.Any())
				return;

			var edgeAgentModules = _moduleLogic.GetEdgeAgentModules(deployments).ToArray();
			var edgeHubProps = _moduleLogic.GetEdgeHubProps(deployments).ToArray();

			_logger.LogInformation("ProcessDeviceAssignment - Building EdgeAgent desired properties.");
			var edgeAgentDesiredProperties = new EdgeAgentDesiredProperties()
			{
				SystemModuleVersion = edgeAgentModules.GetEdgeAgentSchemaVersion(),
				RegistryCredentials = edgeAgentModules.GetEdgeAgentRegCreds().ToList(),
				EdgeModuleSpecifications = edgeAgentModules.GetEdgeAgentModulesSpec(),
				EdgeSystemModuleSpecifications = edgeAgentModules.GetEdgeAgentSystemModulesSpec()
			};

			_logger.LogInformation("ProcessDeviceAssignment - Building EdgeHub desired properties.");
			EdgeHubDesiredProperties edgeHubConfig = new EdgeHubDesiredProperties()
			{
				Routes = edgeHubProps.GetEdgeHubRoutes()
			};

			_logger.LogInformation("ProcessDeviceAssignment - Building ConfigurationContent from desired properties.");
			var configurationContent = new ConfigurationContent()
				.SetEdgeHub(edgeHubConfig)
				.SetEdgeAgent(edgeAgentDesiredProperties);

			var outerModules = _moduleLogic.GetOuterModules(deployments);
			foreach (var outerModule in outerModules)
			{
				if (!outerModule.Value.ContainsKey("properties.desired"))
					continue;

				_logger.LogInformation(
					$"ProcessDeviceAssignment - Building desired properties for module {outerModule.Key}.");
				configurationContent.SetModuleDesiredProperty(new ModuleSpecificationDesiredProperties()
				{
					Name = outerModule.Key,
					DesiredProperties = outerModule.Value["properties.desired"],
				});
			}

			try
			{
				await _ioTHubAccessor.ApplyDeploymentPerDevice(assignment.Key, configurationContent);
			}
			catch (Exception ex)
			{
				_logger.LogError(
					$"Error in ProcessDeviceAssignment - Error applying deployment to device: {ex.Message}");
			}
		}

		private async Task<Dictionary<string, List<DeploymentConfig>>> CreateDeviceDeploymentAssignments(
			Dictionary<string, Configuration> files)
		{
			_logger.LogInformation(
				$"CreateDeviceDeploymentAssignments - Building device/deployments assignments.");

			var assignments = new Dictionary<string, List<DeploymentConfig>>();
			foreach (var config in files)
			{
				var deviceIds = await _ioTHubAccessor.GetDeviceIdsByCondition(config.Value.TargetCondition);
				foreach (var deviceId in deviceIds)
				{
					if (!assignments.ContainsKey(deviceId))
						assignments.Add(deviceId, new List<DeploymentConfig>());

					assignments[deviceId].Add(new DeploymentConfig()
					{
						ManifestFile = config.Key,
						Priority = config.Value.Priority,
						Category = config.Key.Contains(_manifestConfig.DirectoryRootAutomatic)
							? DeploymentCategory.AutomaticDeployment
							: DeploymentCategory.LayeredDeployment,
						ManifestConfig = config.Value
					});
				}
			}

			_logger.LogInformation(
				$"CreateDeviceDeploymentAssignments - Completed, number of devices matching all target conditions: {assignments.Count}.");

			return assignments;
		}

		/// <summary>
		/// Adds a deployment to the file system.
		/// </summary>
		/// <param name="fileName">Deployment file name</param>
		/// <param name="fileContent">Deployment manifest JSON string</param>
		/// <param name="category">Deployment category (automatic, layered)</param>
		/// <exception cref="ArgumentNullException">ArgumentNullException</exception>
		public async Task AddDeployment(string fileName, string fileContent, DeploymentCategory category)
		{
			_logger.LogInformation($"AddDeployment - Adding new deployment - {fileName}.");
			
			if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(fileContent))
			{
				var directory = category == DeploymentCategory.AutomaticDeployment
					? _manifestConfig.DirectoryRootAutomatic
					: _manifestConfig.DirectoryRootLayered;
				var fileLocation = Path.Combine(directory, fileName);
				await File.WriteAllTextAsync(fileLocation, fileContent);
				
				_logger.LogInformation($"AddDeployment - Successfully added new deployment - {fileName}.");
			}
			else
			{
				_logger.LogError($"Error in AddDeployment - number of tasks to run: FileName or FileContent missing.");
				throw new ArgumentNullException("FileName or FileContent missing");
			}
		}

		/// <summary>
		/// Gets content of a single file.
		/// </summary>
		/// <param name="fileName">Relative file path, name of the file with extension.</param>
		/// <param name="category">Deployment category (automatic, layered)</param>
		/// <returns>Dynamic object</returns>
		/// <exception cref="FileNotFoundException">FileNotFoundException</exception>
		public async Task<dynamic> GetFileContent(string fileName, DeploymentCategory category)
		{
			_logger.LogInformation($"GetFileContent - Retrieving file content for file {fileName}.");
			
			var directory = category == DeploymentCategory.AutomaticDeployment
				? _manifestConfig.DirectoryRootAutomatic
				: _manifestConfig.DirectoryRootLayered;
			var fileLocation = Path.Combine(directory, fileName);
			if (!File.Exists(fileLocation))
				throw new FileNotFoundException($"Error in GetFileContent - File {fileName} not found.");

			var content = await File.ReadAllTextAsync(fileLocation);

			_logger.LogInformation($"GetFileContent - Successfully read file content for file {fileName}.");
			return JsonConvert.DeserializeObject<dynamic>(content);
		}

		/// <summary>
		/// Reads all files and returns their contents in a list of configuration objects
		/// </summary>
		/// <param name="directories">Directories of deployment files</param>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException">FileNotFoundException</exception>
		private async Task<Dictionary<string, Configuration>> ReadAllFiles(params string[] directories)
		{
			_logger.LogInformation("ReadAllFiles - Reading layered deployment files");
			
			var fileContents = new Dictionary<string, Configuration>();
			foreach (var directory in directories)
			{
				foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
				{
					if (!File.Exists(file))
						throw new FileNotFoundException($"Error in ReadAllFiles - File {file} not found.");

					var fileContent = await File.ReadAllTextAsync(file);
					fileContent = await LoadSensitiveData(fileContent);
					var config = JsonConvert.DeserializeObject<Configuration>(fileContent);

					fileContents.Add(file, config);
				}
			}

			_logger.LogInformation("ReadAllFiles - Layered deployment files successfully read");
			return fileContents;
		}

		private async Task<string> LoadSensitiveData(string file)
		{
			_logger.LogInformation($"LoadSensitiveData - Retrieving sensitive data from secret store.");
			
			var regexMatch = Regex.Match(file, SecretNameRegEx, RegexOptions.IgnoreCase | RegexOptions.Compiled);

			while (regexMatch.Success)
			{
				var secretName = regexMatch.Groups["secretName"].Value;
				var secretValue = await _keyVaultAccessor.GetSecretByName(secretName);
				file = file.Replace($"{{{{{secretName}}}}}", secretValue);
				regexMatch = regexMatch.NextMatch();
			}

			_logger.LogInformation($"LoadSensitiveData - Successfully loaded sensitive data from secret store.");
			return file;
		}
	}
}