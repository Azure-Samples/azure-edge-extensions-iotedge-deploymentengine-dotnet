using Azure;
using Azure.Security.KeyVault.Secrets;
using IoTEdgeDeploymentEngine;
using IoTEdgeDeploymentEngine.Accessor;
using IoTEdgeDeploymentEngine.Config;
using IoTEdgeDeploymentEngine.Logic;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IoTEdgeDeploymentEngineTests
{
    public class IoTEdgeDeploymentBuilderTests
    {
        private static readonly string RootDirectory = Path.GetFullPath("manifests");

        [Fact()]
        public async Task AddDeployment_Adding_File_Automatic_Test()
        {
            // Arrange 
            IManifestConfig config = GetManifestConfig();
            IModuleLogic logic = new ModuleLogic(Mock.Of<ILogger<ModuleLogic>>());
            var builder = new IoTEdgeDeploymentBuilder(Mock.Of<IIoTHubAccessor>(), null, config, logic, Mock.Of<ILogger<IoTEdgeDeploymentBuilder>>());

            // Act
            await builder.AddDeployment("test.json", "test", DeploymentCategory.AutomaticDeployment);

            // Assert
            string fullFilePath = Path.Combine(config.DirectoryRootAutomatic, "test.json");
            var fileCreated = File.Exists(fullFilePath);

            Assert.True(fileCreated);

            // clean - up file after test
            if (fileCreated)
            {
                File.Delete(fullFilePath);
            }
        }

        [Fact()]
        public async Task AddDeployment_Adding_File_Layered_Test()
        {
            // Arrange 
            IManifestConfig config = GetManifestConfig();
            IModuleLogic logic = new ModuleLogic(Mock.Of<ILogger<ModuleLogic>>());
            var builder = new IoTEdgeDeploymentBuilder(Mock.Of<IIoTHubAccessor>(), null, config, logic, Mock.Of<ILogger<IoTEdgeDeploymentBuilder>>());

            // Act
            await builder.AddDeployment("test.json", "test", DeploymentCategory.LayeredDeployment);

            // Assert
            string fullFilePath = Path.Combine(config.DirectoryRootLayered, "test.json");
            var fileCreated = File.Exists(fullFilePath);

            Assert.True(fileCreated);

            // clean - up file after test
            if (fileCreated)
            {
                File.Delete(fullFilePath);
            }

        }

        [Fact()]
        public async Task GetFileContent_Layered_Test()
        {
            // Arrange 
            IManifestConfig config = GetManifestConfig();
            IModuleLogic logic = new ModuleLogic(Mock.Of<ILogger<ModuleLogic>>());
            var builder = new IoTEdgeDeploymentBuilder(Mock.Of<IIoTHubAccessor>(), null, config, logic, Mock.Of<ILogger<IoTEdgeDeploymentBuilder>>());

            // Act
            var content = await builder.GetFileContent("layered1.json", DeploymentCategory.LayeredDeployment);
            string type = content.GetType().FullName;

            // Assert
            // check if content is
            Assert.True(type.ToLower() == "newtonsoft.json.linq.jobject");

        }

        [Fact()]
        public async Task ApplyDeployments_Test_Normal_Apply()
        {
            // Prepare
            IEnumerable<string> deviceIds = new List<string>() {
                "device100",
                "device200",
            };
            var mockClient = new Mock<IIoTHubAccessor>();
            mockClient.Setup(x => x.GetDeviceIdsByCondition("tags.env='device100'")).ReturnsAsync(deviceIds);
            mockClient.Setup(x => x.GetDeviceIdsByCondition("tags.env='device200' or tags.iiot=true")).ReturnsAsync(deviceIds);
            mockClient.Setup(x => x.ApplyDeploymentPerDevice("device100", It.IsAny<ConfigurationContent>())).Returns(Task.CompletedTask);
            mockClient.Setup(x => x.ApplyDeploymentPerDevice("device200", It.IsAny<ConfigurationContent>())).Returns(Task.CompletedTask);

            var secretMock = new Mock<SecretClient>();
            // Only {{address}} is available to replace for testing in the ./manifests in this project folder
            KeyVaultSecret keyVaultSecret = SecretModelFactory.KeyVaultSecret(new SecretProperties("address"), "address");
            Response<KeyVaultSecret> response = Response.FromValue(keyVaultSecret, Mock.Of<Response>());
            secretMock.Setup(x => x.GetSecretAsync("address", null, It.IsAny<CancellationToken>())).ReturnsAsync(response);
            var keyVaultAccessor = new KeyVaultAccessor(secretMock.Object, Mock.Of<ILogger<KeyVaultAccessor>>());

            IManifestConfig config = GetManifestConfig();
            IModuleLogic logic = new ModuleLogic(Mock.Of<ILogger<ModuleLogic>>());

            var builder = new IoTEdgeDeploymentBuilder(mockClient.Object, keyVaultAccessor, config, logic, Mock.Of<ILogger<IoTEdgeDeploymentBuilder>>());

            // Act
            await builder.ApplyDeployments();

            // Assert
            mockClient.VerifyAll();

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

    }
}