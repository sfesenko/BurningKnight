using System;
using System.Collections.Generic;
using System.IO;
using BurningKnight.entity.creature.drop;
using Lens.assets;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.file;

namespace BurningKnight.assets.loot {
	public static partial class LootTables {
		public static Dictionary<string, Drop> Defined = new Dictionary<string, Drop>();
		public static Dictionary<string, JsonNode> Data = new Dictionary<string, JsonNode>();
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

			root.Write(file);
			file.Close();
		}

		public static void ParseTable(string id, JsonNode table) {
			var drop = ParseDrop(table);

			if (drop == null) {
				return;
			}
			
			Defined[id] = drop;
			Data[id] = table;
		}

		public static JsonNode WriteDrop(Drop drop) {
			var o = new JsonObject();

			o["type"] = drop.GetId();
			drop.Save(o);

			return o;
		}

		public static Drop? ParseDrop(JsonNode? table) {
			var type = table?["type"].String();

			if (type == null) {
				return null;
			}

			if (!DropRegistry.Defined.TryGetValue(type, out var t)) {
				Log.Error($"Unknown drop type {type}");
				return null;
			}

			var drop = (Drop) Activator.CreateInstance(t.Type)!;
			table!["id"] = LastDropId++;
			drop!.Load(table);

			return drop;
		}

	}
}