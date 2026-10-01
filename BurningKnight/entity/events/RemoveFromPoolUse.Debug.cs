using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.entity.item;
using BurningKnight.entity.item.use;
using BurningKnight.save;
using BurningKnight.state;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.events {
	// The editor half of RemoveFromPoolUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class RemoveFromPoolUse {
		public static void RenderDebug(JsonNode root) {
			if (!root["items"].IsJsonArray()) {
				root["items"] = new JsonArray();
			}
			
			var items = root["items"].AsJsonArray();
			var toRemove = -1;
			
			for (var i = 0; i < items!.Count; i++) {
				var item = items[i].AsString();
				
				if (ImGui.InputText($"##item{i}", ref item, 128)) {
					items[i] = item;
				}
				
				ImGui.SameLine();

				if (ImGui.Button("-")) {
					toRemove = i;
				}

				if (!Items.Has(item)) {
					ImGui.BulletText("Unknown item!");
				}	
			}

			if (toRemove > -1) {
				items.Remove(toRemove);
			}

			if (ImGui.Button("+")) {
				items.Add("");
			}
		}
	}
}
