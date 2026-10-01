using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.debug;
using BurningKnight.entity.item;
using BurningKnight.ui.imgui;
using BurningKnight.util;
using ImGuiNET;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.creature.drop {
	// The editor half of PoolDrop; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class PoolDrop {
		public static void RenderDebug(JsonNode root) {
			root.InputFloat("Chance", "chance");

			root.InputInt("Min Count", "min");
			root.InputInt("Max Count", "max");
			
			var pool = root["pool"].Int(0);

			if (ImGui.Combo("Pool##p", ref pool, ItemPool.Names, ItemPool.Count)) {
				root["pool"] = pool;
			}

			if (ImGui.Button("View pool")) {
				WindowManager.PoolEditor = true;
				PoolEditor.Pool = pool;
			}
		}
	}
}
