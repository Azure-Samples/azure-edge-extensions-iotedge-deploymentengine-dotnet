using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace IoTEdgeDeploymentEngine.Accessor
{
	/// <inheritdoc />
	public class KeyVaultAccessor : IKeyVaultAccessor
	{
		private readonly SecretClient _secretClient;
		private readonly ILogger<KeyVaultAccessor> _logger;

		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="secretClient">SecretClient instance</param>
		/// <param name="logger">Logger</param>
		public KeyVaultAccessor(SecretClient secretClient, ILogger<KeyVaultAccessor> logger)
		{
			_secretClient = secretClient;
			_logger = logger;
		}

		/// <inheritdoc />
		public async Task<string> GetSecretByName(string secretName)
		{
			_logger.LogInformation("GetSecretByName - Retrieving secret from Key Vault.");
			
			if (null == _secretClient)
				return string.Empty;
				
			var secret = await _secretClient.GetSecretAsync(secretName);
			
			_logger.LogInformation("GetSecretByName - Successfully obtained secret from Key Vault.");
			return secret?.Value.Value;
		}
	}
}