using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.item.stand;
using BurningKnight.state;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	// The editor half of RerollItemsUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class RerollItemsUse {
		public static void RenderDebug(JsonNode root) {
			var rerollStands = root["r_stands"].Bool(true);
			var spawnNew = root["s_new"].Bool(true);
			var ignore = root["ignore"].Bool(true);

			root.InputFloat("Consume Chance", "cc", 0);
			root.Checkbox("D2", "d2", false);
			
			if (ImGui.Checkbox("Reroll stands", ref rerollStands)) {
				root["r_stands"] = rerollStands;
			}

			if (ImGui.Checkbox("Spawn new", ref spawnNew)) {
				root["s_new"] = spawnNew;
			}

			var tps = root["types"];

			if (!tps.IsJsonArray()) {
				tps = root["types"] = new JsonArray();
			}

			if (ImGui.TreeNode("Item types")) {
				if (ImGui.Checkbox("Ignore those types", ref ignore)) {
					root["ignore"] = ignore;
				}
				
				ImGui.Separator();
				
				var tp = tps.AsJsonArray();
				var toRemove = -1;
				var toAdd = -1;

				for (var i = 0; i < ItemEditor.Types.Length; i++) {
					var v = tp!.Contains(i);

					if (ImGui.Checkbox(ItemEditor.Types[i], ref v)) {
						if (v) {
							toAdd = i;
						} else {
							toRemove = tp.IndexOf(i);
						}
					}
				}

				if (toRemove != -1) {
					tp!.Remove(toRemove);
				} else if (toAdd != -1) {
					tp!.Add(toAdd);
				}
				
				ImGui.TreePop();
			}
		}
	}
}
