using System;
using System.Collections.Generic;
using BurningKnight.level.biome;
using Lens.assets;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.util {
	public static class BiomeTitles {
		// Localised through loading_biome_<id>_0..N in the locale files
		// (see LoadScreenJokes for the fallback rule). Counts below must
		// match the number of keys per biome in en.json.
		private static Dictionary<string, int> counts = new Dictionary<string, int>() {
			{ Biome.Hub, 2 },
			{ Biome.Castle, 2 },
			{ Biome.Desert, 4 },
			{ Biome.Jungle, 4 },
			{ Biome.Ice, 7 },
			{ Biome.Library, 6 },
			{ Biome.Tech, 7 },
			{ Biome.Cave, 5 }
		};

		public static string Generate(string biome) {
			if (!counts.TryGetValue(biome, out var n)) {
				Log.Error($"Didn't find title for {biome}");
				return "Idk man, kinda 404?";
			}

			return Locale.Get($"loading_biome_{biome}_{new Random().Next(n)}");
		}
	}
}
