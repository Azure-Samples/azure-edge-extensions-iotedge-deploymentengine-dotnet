using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;

namespace IoTEdgeDeploymentApi.Security
{
    /// <summary>
    /// Open Api Http Trigger Auth that returns a response depending on claims
    /// </summary>
    public class OpenApiHttpTriggerAuthorization : DefaultOpenApiHttpTriggerAuthorization
    {
        /// <summary>
        /// Http context accessor
        /// </summary>
        public virtual IHttpContextAccessor HttpContextAccessor { get; set; }

        /// <inheritdoc />
        public override async Task<OpenApiAuthorizationResult> AuthorizeAsync(IHttpRequestDataObject req)
        {
            var result = default(OpenApiAuthorizationResult);

            var claims = this.HttpContextAccessor.HttpContext.User.Claims;
            if (!claims.Any())
            {
                //todo: assess claims for Swagger UI auth
                // result = new OpenApiAuthorizationResult()
                // {
                //     StatusCode = HttpStatusCode.Unauthorized,
                //     ContentType = "text/plain",
                //     Payload = "Unauthorized",
                // };

                return await Task.FromResult(result).ConfigureAwait(false);
            }

            return await Task.FromResult(result).ConfigureAwait(false);
        }
    }
}