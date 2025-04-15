using System.Runtime.CompilerServices;

namespace Aseprite {
	public static class Calc {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsBitSet(uint b, int pos) {
			return (b & (1 << pos)) != 0;
		}
	}
}