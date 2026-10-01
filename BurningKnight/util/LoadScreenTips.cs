using System;
using Lens.assets;

namespace BurningKnight.util {
	public static class LoadScreenTips {
		// Localised through loading_tip_0..Count-1 (see LoadScreenJokes).
		public const int Count = 6;

		public static string Generate() {
			return Locale.Get($"loading_tip_{new Random().Next(Count)}");
		}
	}
}
