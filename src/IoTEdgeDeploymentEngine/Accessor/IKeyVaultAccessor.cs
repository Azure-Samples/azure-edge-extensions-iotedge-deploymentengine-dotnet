using System.Threading.Tasks;

namespace IoTEdgeDeploymentEngine.Accessor
{
	/// <summary>
	/// Accesses KeyVault instance as secret store by SecretClient
	/// </summary>
	public interface IKeyVaultAccessor
	{
		/// <summary>
		/// Gets secret value from KeyVault
		/// </summary>
		/// <param name="secretName">Name of the secret</param>
		/// <returns></returns>
		Task<string> GetSecretByName(string secretName);
	}
}