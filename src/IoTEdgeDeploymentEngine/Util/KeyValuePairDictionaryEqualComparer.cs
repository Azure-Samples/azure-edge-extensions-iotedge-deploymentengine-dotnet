using System.Collections.Generic;

namespace IoTEdgeDeploymentEngine.Util
{
	/// <summary>
	/// EqualityComparer for KeyValuePair
	/// </summary>
	public class KeyValuePairDictionaryEqualComparer : IEqualityComparer<KeyValuePair<string, IDictionary<string, object>>>
	{
		/// <inheritdoc />
		public bool Equals(KeyValuePair<string, IDictionary<string, object>> x, KeyValuePair<string, IDictionary<string, object>> y)
		{
			return x.Key == y.Key;
		}

		/// <inheritdoc />
		public int GetHashCode(KeyValuePair<string, IDictionary<string, object>> obj)
		{
			return obj.Key.GetHashCode();
		}
	}
}

