using System;
using System.Collections.Generic;
using System.Text;

namespace IoTEdgeDeploymentEngine.Config
{
    /// <inheritdoc />
    public class ManifestConfig : IManifestConfig
    {
        /// <inheritdoc />
        public string DirectoryRootAutomatic { get; set; }

        /// <inheritdoc />
        public string DirectoryRootLayered { get; set; }
    }
}
