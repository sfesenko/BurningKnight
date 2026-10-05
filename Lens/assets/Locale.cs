using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.file;

namespace Lens.assets {
	public class Locale {
		public static Dictionary<string, string> Map = null!; // Load() sets it
		
		public static Dictionary<string, string> Fallback = new();
		public static readonly Dictionary<string, Dictionary<string, string>> Loaded = new();
		private static bool LoadedFallback;
		
		public static string Current = null!; // SetLanguage sets it
		public static string PrefferedClientLanguage = "en";

		private static readonly string[] quacks = ["quack", "QUACK", "quaaak", "qk"];

		private static void LoadRaw(string name, string path, bool backup = false) {
			if (Loaded.TryGetValue(name, out var cached)) {
				Map = cached;
				return;
			}

			if (name == "qu") {
				cached = new Dictionary<string, string>();
				Loaded[name] = cached;

				var i = 0;

				foreach (var entry in Fallback) {
					cached[entry.Key] = Regex.Replace(entry.Value, @"\w+(?<!^\[)\b", (m) => {
						i++;
						return char.IsUpper(m.Value[0]) ? "Quack" : quacks[(m.Value[0] + i * 5) % quacks.Length];
					});
				}
				
				return;
			}
			
			var file = FileHandle.FromRoot(path);

			if (!file.Exists()) {
				Log.Error($"Locale {path} was not found!");
				return;
			}

			try {
				var root = JsonNode.Parse(file.ReadAll());
				
				cached = new Dictionary<string, string>();
				Loaded[name] = cached;

				foreach (var entry in root.AsJsonObject()!) {
					cached[entry.Key] = entry.Value.AsString()!;
				}

				if (backup) {
					Fallback = cached;
				} else {
					Loaded[name] = cached;
					Map = cached;
				}
			} catch (Exception e) {
				Log.Error(e);
			}
		}
		
		public static void Load(string locale) {
			if (!LoadedFallback) {
				LoadRaw("en", "Locales/en.json", true);
				LoadedFallback = true;
			} 
			
			if (Current == locale) {
				return;
			}
			
			Current = locale;

			if (!Loaded.ContainsKey(locale)) {
				LoadRaw(locale, $"Locales/{locale}.json");
			}

			if (Loaded.TryGetValue(locale, out var value)) {
				Map = value;
			}
		}

		public static void Save() {
			Log.Info($"Saving locale {Current}");
			
			try {
				var file = Assets.WriteContent($"Locales/{Current}.json");

				if (file == null) {
					return;
				}

				var root = new JsonObject();

				foreach (var t in Map) {
					root[t.Key] = t.Value;
				}

				root.Write(file,
					#if DEBUG
						true
					#else
						false
					#endif
					);
				file.Close();
			} catch (Exception e) {
				Log.Error(e);
			}
		}

		public static void Delete() {
			try {
				FileHandle.FromRoot($"Locales/{Current}.json").Delete();
				Current = "en";
			} catch (Exception e) {
				Log.Error(e);
			}
		}

		public static string Get(string key, bool eng = false) {
			// Map stays null until Assets.Load reaches the locale; the loading screen asks for a
			// tip before that, so a miss falls back to English and then to the key itself.
			if (eng || Map == null || !Map.TryGetValue(key, out var value)) {
				return GetEnglish(key);
			}

			return value;
		}
		
		public static string GetEnglish(string key) {
			return Fallback.GetValueOrDefault(key, key);
		}

		public static bool Contains(string key) {
			// Like Get: the loading screen can ask before Assets.Load reached the locale.
			return (Map?.ContainsKey(key) ?? false) || Fallback.ContainsKey(key);
		}
	}
}