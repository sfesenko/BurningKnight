using System;
using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.entity.component;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.item.use {
	// The editor half of SpawnItemsUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class SpawnItemsUse {
		public static void RenderDebug(JsonNode root) {
			var toRemove = -1;
			
			if (!root["items"].IsJsonArray()) {
				root["items"] = new JsonArray();
			}
			
			var toSpawn = root["items"].AsJsonArray();

			for (var i = 0; i < toSpawn!.Count; i++) {
				var item = toSpawn![i];
				var v = item![0].Int(1);
				var n = item![1].String("");

				if (ImGui.InputText($"##ss{i}", ref n, 128)) {
					item[1] = n;
				}
				
				ImGui.SameLine();

				if (ImGui.InputInt($"##sl{i}", ref v)) {
					item[0] = v;
				}
				
				ImGui.SameLine();
				
				if (ImGui.Button("-")) {
					toRemove = i;
				}
			}

			if (ImGui.Button("+")) {
				toSpawn.Add(new JsonArray {
					1, "bk:copper_coin"
				});
			}

			if (toRemove > -1) {
				toSpawn.Remove(toRemove);
			}
		}
	}
}
