using BurningKnight.assets.particle.custom;
using BurningKnight.entity.component;
using ImGuiNET;
using Lens.assets;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyStatUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyStatUse {
		public static void RenderDebug(JsonNode root) {
			var stat = root["stat"].Int(0);

			if (ImGui.Combo("Stat", ref stat, stats, stats.Length)) {
				root["stat"] = stat;
			}
			
			var value = root["val"].Number(1);

			if (ImGui.InputFloat("Amount", ref value)) {
				root["val"] = value;
			}
		}
	}
}
