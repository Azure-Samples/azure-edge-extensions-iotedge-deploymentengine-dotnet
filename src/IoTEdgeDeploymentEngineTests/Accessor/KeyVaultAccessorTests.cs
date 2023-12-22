using Azure;
using Azure.Security.KeyVault.Secrets;
using IoTEdgeDeploymentEngine.Accessor;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IoTEdgeDeploymentEngineTests.Accessor
{
    public class KeyVaultAccessorTests
    {
        [Fact()]
        public async Task GetSecretByName_Test()
        {
            var secretMock = new Mock<SecretClient>();

            KeyVaultSecret keyVaultSecret = SecretModelFactory.KeyVaultSecret(new SecretProperties("address"), "address");
            Response<KeyVaultSecret> response = Response.FromValue(keyVaultSecret, Mock.Of<Response>());

            secretMock.Setup(x => x.GetSecretAsync("address", null, It.IsAny<CancellationToken>())).ReturnsAsync(response);

            var keyVaultAccessor = new KeyVaultAccessor(secretMock.Object, Mock.Of<ILogger<KeyVaultAccessor>>());
            // Act
            var secret = await keyVaultAccessor.GetSecretByName("address");

            // Assert
            Assert.Equal("address", secret);

        }
    }
}