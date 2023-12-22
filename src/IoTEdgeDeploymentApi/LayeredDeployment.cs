using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using IoTEdgeDeploymentApi.Model;
using IoTEdgeDeploymentApi.Schema;
using IoTEdgeDeploymentApi.Security;
using IoTEdgeDeploymentEngine;
using IoTEdgeDeploymentEngine.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace IoTEdgeDeploymentApi
{
	/// <summary>
	/// Http controller for layered deployment
	/// </summary>
	public class LayeredDeployment
	{
		private readonly IIoTEdgeDeploymentBuilder _ioTEdgeDeploymentBuilder;
		private readonly ILogger<LayeredDeployment> _logger;
		private readonly IJwtValidator _jwtValidator;
		private readonly IDeploymentManifestValidator _validator;

		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="ioTEdgeDeploymentBuilder">IoTEdgeDeploymentBuilder instance per DI</param>
		/// <param name="logger">ILogger instance per DI</param>
		/// <param name="jwtValidator">Jwt Validator</param>
		/// <param name="validator">Schema validator</param>
		public LayeredDeployment(IIoTEdgeDeploymentBuilder ioTEdgeDeploymentBuilder, ILogger<LayeredDeployment> logger,
			IJwtValidator jwtValidator, IDeploymentManifestValidator validator)
		{
			_ioTEdgeDeploymentBuilder = ioTEdgeDeploymentBuilder;
			_logger = logger;
			_jwtValidator = jwtValidator;
			_validator = validator;
		}

		/// <summary>
		/// Creates a new layered deployment manifest file.
		/// </summary>
		/// <param name="req">Http request</param>
		/// <returns></returns>
		[FunctionName("AddLayeredDeployment")]
		[OpenApiOperation(operationId: "AddLayeredDeployment", tags: new[] { "IoTEdgeLayeredDeployment" })]
		[OpenApiRequestBody(contentType: "application/json", bodyType: typeof(DeploymentFile), Required = true,
			Description = "Specifies a file name with file extension and content for the deployment manifest")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
			bodyType: typeof(string), Description = "The OK response")]
		[OpenApiSecurity("implicit_auth", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow))]
		[OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer,
			BearerFormat = "JWT")]
		public async Task<IActionResult> AddLayeredDeployment(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
			HttpRequest req)
		{
			try
			{
				if (null == _jwtValidator.Validate(req.Headers["Authorization"].ToString()))
					return new UnauthorizedObjectResult("401 - Unauthorized to call this function.");

				var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
				var data = JsonConvert.DeserializeObject<DeploymentFile>(requestBody);
				if (!_validator.Validate(data.FileContent, out var message))
				{
					return new BadRequestErrorMessageResult($"Schema validation failed with the following errors - {message}");
				}
				
				await _ioTEdgeDeploymentBuilder.AddDeployment(data?.FileName, data?.FileContent,
					DeploymentCategory.LayeredDeployment);

				return new OkObjectResult("Succeeded");
			}
			catch (System.Exception ex)
			{
				return new BadRequestErrorMessageResult(ex.Message);
			}
		}

		/// <summary>
		/// Gets the content of a layered deployment manifest file.
		/// </summary>
		/// <param name="req">Http request</param>
		/// <returns></returns>
		[FunctionName("GetLayeredDeploymentFileContent")]
		[OpenApiOperation(operationId: "GetLayeredDeploymentFileContent", tags: new[] { "IoTEdgeLayeredDeployment" })]
		[OpenApiParameter(name: "fileName", In = ParameterLocation.Query, Required = true, Type = typeof(string),
			Description = "The **fileName** parameter")]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
			bodyType: typeof(string), Description = "The OK response")]
		[OpenApiSecurity("implicit_auth", SecuritySchemeType.OAuth2, Flows = typeof(ImplicitAuthFlow))]
		[OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer,
			BearerFormat = "JWT")]
		public async Task<IActionResult> GetLayeredDeploymentFileContent(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
			HttpRequest req)
		{
			try
			{
				if (null == _jwtValidator.Validate(req.Headers["Authorization"].ToString()))
					return new UnauthorizedObjectResult("401 - Unauthorized to call this function.");
				
				var fileName = req.Query["fileName"];
				if(!Regex.Match(fileName, @"^[a-z_\-\s0-9\.]+\.(json)$", RegexOptions.IgnoreCase).Success)
				{
					_logger.LogError($"GetLayeredDeploymentFileContent error: fileName is not a valid .json file name");
                	return new BadRequestErrorMessageResult($"Error: fileName '{fileName}' is not a valid .json file name"); 
				}
				
				var content = await _ioTEdgeDeploymentBuilder.GetFileContent(fileName, DeploymentCategory.LayeredDeployment);
				return new OkObjectResult(content);
			}
			catch (System.Exception ex)
			{
                _logger.LogError($"GetLayeredDeploymentFileContent exception: {ex.Message}");
                return new BadRequestErrorMessageResult(ex.Message);
			}
		}
	}
}