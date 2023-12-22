using System.Collections.Generic;

namespace IoTEdgeDeploymentEngine.Util
{
	/// <summary>
	/// EqualityComparer for KeyValuePair
	/// </summary>
	public class KeyValuePairEqualComparer : IEqualityComparer<KeyValuePair<string, object>>
	{
		/// <inheritdoc />
		public bool Equals(KeyValuePair<string, object> x, KeyValuePair<string, object> y)
		{
			return x.Key == y.Key;
		}

		/// <inheritdoc />
		public int GetHashCode(KeyValuePair<string, object> obj)
		{
			return obj.Key.GetHashCode();
		}
	}
}

