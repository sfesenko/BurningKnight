using BurningKnight.entity.events;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of MakeProjectilesBounceUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class MakeProjectilesBounceUse {
		public static void RenderDebug(JsonNode root) {
			var count = root["count"].Int(1);

			if (ImGui.InputInt("Count", ref count)) {
				root["count"] = count;
			}
		}
	}
}
