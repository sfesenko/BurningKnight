using System.Collections.Generic;
using BurningKnight.entity.creature.drop;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of SpawnDropUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class SpawnDropUse {
		public static void RenderDebug(JsonNode root) {
			root.InputText("Drop", "drop");
			
			if (!assets.loot.Drops.Defined.ContainsKey(drop)) {
				ImGui.Text($"Unknown drop {drop}");
			}
		}
	}
}
