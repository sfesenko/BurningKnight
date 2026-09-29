using System.Collections.Generic;
using BurningKnight.assets.loot;
using BurningKnight.util;
using ImGuiNET;
using Lens.lightJson;
using Lens.util.math;

namespace BurningKnight.entity.creature.drop {
	// The editor half of OneOfDrop; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class OneOfDrop {
		public static void RenderDebug(JsonValue root) {
			root.InputFloat("Chance", "chance");
			
			if (!root["drops"].IsJsonArray) {
				root["drops"] = new JsonArray();
			}

			var drops = root["drops"].AsJsonArray;
			var toRemove = -1;
			
			for (var i = 0; i < drops.Count; i++) {
				if (LootTables.RenderDrop(drops[i]) && ImGui.Button("Remove drop")) {
					toRemove = i;
				}
			}

			if (toRemove > -1) {
				drops.Remove(toRemove);
			}

			if (DropNames == null) {
				DropNames = new string[DropRegistry.Defined.Count];
				var i = 0;

				foreach (var v in DropRegistry.Defined.Values) {
					DropNames[i++] = v.Id;
				}
			}
			
			ImGui.Separator();

			ImGui.Combo("##tp", ref CurrentDrop, DropNames, DropNames.Length);
			ImGui.SameLine();
			
			if (ImGui.Button("Add drop")) {
				drops.Add(new JsonObject {
					["type"] = DropNames[CurrentDrop]
				});
			}
		}
	}
}
