using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.controller;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of MakeProjectilesSlowDown; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class MakeProjectilesSlowDown {
		public static void RenderDebug(JsonValue root) {
			var val = root["amount"].Number(1);

			if (ImGui.InputFloat("Speed", ref val)) {
				root["amount"] = val;
			}
			
			val = root["time"].Number(1);

			if (ImGui.InputFloat("Time", ref val)) {
				root["time"] = val;
			}
		}
	}
}
