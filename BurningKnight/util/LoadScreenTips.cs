using System;
using Lens.assets;
using Lens.input;

namespace BurningKnight.util {
	public static class LoadScreenTips {
		// Localised through loading_tip_0..Count-1 (see LoadScreenJokes).
		public const int Count = 6;

		public static string Generate() {
			var index = new Random().Next(Count);

			// Press R for a surprise: no keyboard on a handheld, resample instead of lying.
			while (index == 3 && !TextInput.Available) {
				index = new Random().Next(Count);
			}

			return Locale.Get($"loading_tip_{index}");
		}
	}
}
