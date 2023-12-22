using IoTEdgeDeploymentEngine.Config;
using IoTEdgeDeploymentEngine.Logic;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace IoTEdgeDeploymentEngineTests.Logic
{
	public class ModuleLogicTests
	{
		private static readonly string RootDirectory = Path.GetFullPath("manifests");

		private readonly Mock<ILogger<ModuleLogic>> _loggerMock;
		private readonly IModuleLogic _sut;
		private readonly Dictionary<string, Configuration> _configsAutomatic;
		private readonly Dictionary<string, Configuration> _configsLayered;
		private KeyValuePair<string,List<DeploymentConfig>> _defaultAssignments;

		public ModuleLogicTests()
		{
			_loggerMock = new Mock<ILogger<ModuleLogic>>();
			_sut = new ModuleLogic(_loggerMock.Object);

			var manifestConfig = GetManifestConfig();
			_configsAutomatic = ReadAllFiles(manifestConfig.DirectoryRootAutomatic).GetAwaiter().GetResult();
			_configsLayered = ReadAllFiles(manifestConfig.DirectoryRootLayered).GetAwaiter().GetResult();
			_defaultAssignments = InitDefaultAssignments();
		}

		[Fact]
		public void SelectDeployments_NoAutomaticDeployments()
		{
			var assignment = new KeyValuePair<string, List<DeploymentConfig>>("Device1", new List<DeploymentConfig>
			{
				new()
				{
					Category = DeploymentCategory.LayeredDeployment,
					Priority = 1
				}
			});

			var actual = _sut.SelectDeployments(assignment);

			Assert.Empty(actual);
		}

		[Theory]
		[InlineData(1, 2, 2, 3)]
		[InlineData(3, 2, 2, 1)]
		public void SelectDeployments_CheckPriority(int prio1, int prio2, int prio3, int expectedResult)
		{
			var assignments = new KeyValuePair<string, List<DeploymentConfig>>("device100", new List<DeploymentConfig>
			{
				new()
				{
					Category = DeploymentCategory.AutomaticDeployment,
					Priority = prio1,
					ManifestConfig = _configsAutomatic.FirstOrDefault().Value
				},
				new()
				{
					Category = DeploymentCategory.LayeredDeployment,
					Priority = prio2,
					ManifestConfig = _configsLayered.FirstOrDefault().Value
				},
				new()
				{
					Category = DeploymentCategory.LayeredDeployment,
					Priority = prio3,
					ManifestConfig = _configsLayered.FirstOrDefault().Value
				}
			});

			var actual = _sut.SelectDeployments(assignments).ToArray();

			Assert.Equal(expectedResult, actual.Length);
		}

		[Fact]
		public void GetEdgeAgentModules_Test()
		{
			var deployments = _sut.SelectDeployments(_defaultAssignments).ToArray();
			var actual = _sut.GetEdgeAgentModules(deployments).ToArray();
			Assert.NotEmpty(actual);
			var grouped = actual.GroupBy(a => a.Key);
			foreach (var module in grouped)
			{
				// Assert --> unique entry per module
				Assert.Equal(1, module.Count());
			}
		}		
		
		[Fact]
		public void GetEdgeHubModules_Test()
		{
			_defaultAssignments = InitDefaultAssignments();
			var deployments = _sut.SelectDeployments(_defaultAssignments).ToArray();
			var actual = _sut.GetEdgeHubProps(deployments).ToArray();
			Assert.NotEmpty(actual);
			var grouped = actual.GroupBy(a => a.Key);
			foreach (var module in grouped)
			{
				// Assert --> unique entry per route
				Assert.Equal(1, module.Count());
			}
		}	
		
		[Fact]
		public void GetOuterModules_Test()
		{
			_defaultAssignments = InitDefaultAssignments();
			var deployments = _sut.SelectDeployments(_defaultAssignments).ToArray();
			var actual = _sut.GetOuterModules(deployments).ToArray();
			Assert.NotEmpty(actual);
			var grouped = actual.GroupBy(a => a.Key);
			foreach (var module in grouped)
			{
				// Assert --> unique entry per route
				Assert.Equal(1, module.Count());
			}
		}

		private KeyValuePair<string, List<DeploymentConfig>> InitDefaultAssignments()
		{
			var assignments = new KeyValuePair<string, List<DeploymentConfig>>("device100", new List<DeploymentConfig>
			{
				new DeploymentConfig
				{
					Category = DeploymentCategory.AutomaticDeployment,
					Priority = 1,
					ManifestConfig = _configsAutomatic.FirstOrDefault().Value
				},
				new DeploymentConfig
				{
					Category = DeploymentCategory.LayeredDeployment,
					Priority = 2,
					ManifestConfig = _configsLayered.FirstOrDefault().Value
				},
				new DeploymentConfig
				{
					Category = DeploymentCategory.LayeredDeployment,
					Priority = 2,
					ManifestConfig = _configsLayered.LastOrDefault().Value
				}
			});
			return assignments;
		}

		private IManifestConfig GetManifestConfig()
		{
			IManifestConfig manifestConfig = new ManifestConfig()
			{
				DirectoryRootAutomatic = Path.Combine(RootDirectory, "AutomaticDeployment"),
				DirectoryRootLayered = Path.Combine(RootDirectory, "LayeredDeployment")
			};

			return manifestConfig;
		}

		private async Task<Dictionary<string, Configuration>> ReadAllFiles(params string[] directories)
		{
			var fileContents = new Dictionary<string, Configuration>();
			foreach (var directory in directories)
			{
				foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
				{
					var fileContent = await File.ReadAllTextAsync(file);
					var config = JsonConvert.DeserializeObject<Configuration>(fileContent);

					fileContents.Add(file, config);
				}
			}

			return fileContents;
		}
	}
}