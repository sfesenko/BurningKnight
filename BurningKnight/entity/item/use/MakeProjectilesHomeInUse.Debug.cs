using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.controller;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of MakeProjectilesHomeInUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class MakeProjectilesHomeInUse {
		public static void RenderDebug(JsonNode root) {
			var speed = root["speed"].Number(1);

			if (ImGui.InputFloat("Speed", ref speed)) {
				root["speed"] = speed;
			}

			root.Checkbox("Better?", "better", false);
		}
	}
}
