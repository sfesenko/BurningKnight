using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.controller;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of MakeProjectileShrinkUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class MakeProjectileShrinkUse {
		public static void RenderDebug(JsonValue root) {
			var speed = root["speed"].Number(1);

			if (ImGui.InputFloat("Speed", ref speed)) {
				root["speed"] = speed;
			}
		}
	}
}
