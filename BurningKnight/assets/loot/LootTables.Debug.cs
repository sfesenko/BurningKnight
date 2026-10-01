using System;
using System.Collections.Generic;
using System.IO;
using BurningKnight.entity.creature.drop;
using ImGuiNET;
using Lens.assets;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.file;

namespace BurningKnight.assets.loot {
	// The editor half of LootTables; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class LootTables {
		public static bool RenderDrop(JsonNode drop) {
			var id = drop["type"].String("missing");

			if (DropRegistry.Defined.TryGetValue(id, out var info)) {
				if (ImGui.TreeNode($"{id}%##{drop["id"].AsInteger()}")) {
					info.Render(drop);
					ImGui.TreePop();

					return true;
				}
			} else {
				ImGui.BulletText($"Unknown type {id}");
			}

			return false;
		}
	}
}
