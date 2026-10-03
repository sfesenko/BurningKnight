using System;
using Lens.assets;
using Lens.input;

namespace BurningKnight.util {
	public static class LoadScreenJokes {
		// Localised through loading_joke_0..Count-1 in the locale files.
		// en.json is the fallback: Locale.Get returns English for locales
		// that have not translated a key yet, so behaviour is unchanged
		// until translations land. Bump Count when adding jokes.
		public const int Count = 78;

		public static string Generate() {
			var index = new Random().Next(Count);

			// Press / to open chat: no keyboard on a handheld, resample instead of lying.
			while (index == 4 && !TextInput.Available) {
				index = new Random().Next(Count);
			}

			return Locale.Get($"loading_joke_{index}");
		}
	}
}
