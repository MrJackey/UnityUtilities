using System.Diagnostics.Contracts;

namespace Jackey.Behaviours.Utilities {
	public static class InfoUtilities {
		public const char MULTI_INFO_SEPARATOR = '↳';

		[Pure]
		public static string AlignCenter(string text) {
			return $"<align=\"center\">{text}</align>";
		}
	}
}
