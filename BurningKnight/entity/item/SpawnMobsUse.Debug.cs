using System;
using BurningKnight.assets.particle;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.item.use;
using BurningKnight.state;
using BurningKnight.util;
using ImGuiNET;
using Lens.assets;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;
using Lens.util.timer;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.item {
	// The editor half of SpawnMobsUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class SpawnMobsUse {
		public static void RenderDebug(JsonNode root) {
			var v = root["count"].Int(1);

			if (ImGui.InputInt("Count", ref v)) {
				root["count"] = v;
			}
		}
	}
}
