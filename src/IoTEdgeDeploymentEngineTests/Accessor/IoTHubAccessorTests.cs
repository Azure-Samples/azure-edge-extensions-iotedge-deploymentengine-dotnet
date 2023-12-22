using IoTEdgeDeploymentEngine.Accessor;
using IoTEdgeDeploymentEngine.Extension;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Polly.Registry;
using Xunit;

namespace IoTEdgeDeploymentEngineTests.Accessor;

public class IoTHubAccessorTests
{
	private readonly PolicyRegistry _policyRegistry;
	private readonly Mock<RegistryManager> _registryManager;

	public IoTHubAccessorTests()
	{
		_policyRegistry = new PolicyRegistry()
			.AddExponentialBackoffRetryPolicy()
			.AddInfiniteRetryPolicy()
			.AddCircuitBreakerPolicy();
		
		_registryManager = new Mock<RegistryManager>();
	}

	[Fact()]
	public async Task GetDeviceIdsByCondition_Test()
	{
		var twins = new List<Twin>
		{
			new()
			{
				DeviceId = "device100"
			},
			new()
			{
				DeviceId = "device200"
			}
		};

		var query = new Mock<IQuery>();
		query.Setup(x => x.HasMoreResults).Returns(true).Callback(() =>
		{
			query.Setup(y => y.HasMoreResults).Returns(false);
		});
		query.Setup(x => x.GetNextAsTwinAsync()).ReturnsAsync(twins);

		_registryManager.Setup(x => x.CreateQuery(It.IsAny<string>())).Returns(query.Object);

		var sut = new IoTHubAccessor(_registryManager.Object, _policyRegistry, Mock.Of<ILogger<IoTHubAccessor>>());
		var actual = (await sut.GetDeviceIdsByCondition("sql")).ToArray();

		Assert.Equal(twins.Count, actual.Count());
		Assert.True(twins.All(t => actual.Contains(t.DeviceId)));
		Assert.Equal(twins.Count, actual.Count());
	}

	[Fact()]
	public async Task ApplyDeploymentPerDevice_Test()
	{
		_registryManager
			.Setup(r => r.ApplyConfigurationContentOnDeviceAsync(It.IsAny<string>(), It.IsAny<ConfigurationContent>()))
			.Returns(Task.CompletedTask);
		
		var sut = new IoTHubAccessor(_registryManager.Object, _policyRegistry, Mock.Of<ILogger<IoTHubAccessor>>());
		await sut.ApplyDeploymentPerDevice("device100", new ConfigurationContent());

		_registryManager.Verify(
			r => r.ApplyConfigurationContentOnDeviceAsync(It.IsAny<string>(), It.IsAny<ConfigurationContent>()),
			Times.Once);
	}
}