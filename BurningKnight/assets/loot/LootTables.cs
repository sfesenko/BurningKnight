using System;
using System.Collections.Generic;
using System.IO;
using BurningKnight.entity.creature.drop;
using Lens.assets;
using Lens.lightJson;
using Lens.lightJson.Serialization;
using Lens.util;
using Lens.util.file;

namespace BurningKnight.assets.loot {
	public static partial class LootTables {
		public static Dictionary<string, Drop> Defined = new Dictionary<string, Drop>();
		public static Dictionary<string, JsonValue> Data = new Dictionary<string, JsonValue>();
		public static int LastDropId;

		public static void Load() {
			// Disabled: loot tables are not loaded from disk — drops are defined on the items.
			// The loader was unreachable and was removed; git has it if it ever comes back.
		}

		public static void Save() {
			Log.Info("Saving loot tables");
			
			var root = new JsonObject();

			foreach (var d in Data) {
				root[d.Key] = d.Value;
			}
			
			var file = Assets.WriteContent("Loot/loot.json");

			if (file == null) {
				return;
			}

			var writer = new JsonWriter(file);
			writer.Write(root);
			file.Close();
		}

		public static void ParseTable(string id, JsonValue table) {
			var drop = ParseDrop(table);

			if (drop == null) {
				return;
			}
			
			Defined[id] = drop;
			Data[id] = table;
		}

		public static JsonValue WriteDrop(Drop drop) {
			var o = new JsonObject();

			o["type"] = drop.GetId();
			drop.Save(o);

			return o;
		}

		public static Drop ParseDrop(JsonValue table) {
			var type = table["type"].String(null);

			if (type == null) {
				return null;
			}

			if (!DropRegistry.Defined.TryGetValue(type, out var t)) {
				Log.Error($"Unknown drop type {type}");
				return null;
			}

			var drop = (Drop) Activator.CreateInstance(t.Type);
			table["id"] = LastDropId++;
			drop.Load(table);

			return drop;
		}

	}
}