using UnityEngine;

namespace Jackey.Utilities.Extensions {
	public static class LayerMaskExtensions {
		/// <summary>
		/// Checks whether or not the LayerMask contains the input layer
		/// </summary>
		/// <returns>Returns true if the mask contains the layer</returns>
		public static bool Includes(this LayerMask mask, int layer) {
			return (mask.value & (1 << layer)) != 0;
		}
	}
}
