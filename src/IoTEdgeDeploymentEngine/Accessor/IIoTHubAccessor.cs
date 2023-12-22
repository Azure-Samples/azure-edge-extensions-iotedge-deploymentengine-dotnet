using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace IoTEdgeDeploymentEngine.Accessor
{
    /// <summary>
    /// Accesses IoTHub instance by using the RegistryManager and IoTHub SDK
    /// </summary>
    public interface IIoTHubAccessor
    {
        /// <summary>
        /// Queries the device ids by target condition from IoTHub 
        /// </summary>
        /// <param name="targetCondition">Tags based target condition</param>
        /// <returns></returns>
        Task<IEnumerable<string>> GetDeviceIdsByCondition(string targetCondition);
        
        /// <summary>
        /// Applies the deployment to a device
        /// </summary>
        /// <param name="deviceId">Device id</param>
        /// <param name="configurationContent">Configuration content</param>
        /// <returns></returns>
        Task ApplyDeploymentPerDevice(string deviceId, ConfigurationContent configurationContent);
    }
}