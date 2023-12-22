using System;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.OpenApi.Models;

namespace IoTEdgeDeploymentApi.Security
{
    /// <summary>
    /// Implicit Authentication Flow Setup
    /// </summary>
    public class ImplicitAuthFlow : OpenApiOAuthSecurityFlows
    {
        private const string AuthorisationUrl = "https://login.microsoftonline.com/{0}/oauth2/v2.0/authorize";
        private const string RefreshUrl = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

        /// <summary>
        /// ctor
        /// </summary>
        public ImplicitAuthFlow()
        {
            var tenantId = Environment.GetEnvironmentVariable("OpenApi__Auth__TenantId");
            var defaultScope = Environment.GetEnvironmentVariable("OpenApi__Auth__Scope");

            this.Implicit = new OpenApiOAuthFlow()
            {
                AuthorizationUrl = new Uri(string.Format(AuthorisationUrl, tenantId)),
                RefreshUrl = new Uri(string.Format(RefreshUrl, tenantId)),

                Scopes =
                {
                    {
                        defaultScope ?? string.Empty, "Default scope defined in the app"
                    }
                }
            };
        }
    }
}