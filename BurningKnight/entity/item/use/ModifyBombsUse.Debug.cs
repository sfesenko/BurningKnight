using System;
using BurningKnight.entity.bomb;
using BurningKnight.entity.component;
using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyBombsUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyBombsUse {
		public static void RenderDebug(JsonNode root) {
			var spawnBullets = root["spawn_bullets"].Bool(false);

			if (ImGui.Checkbox("Spawn Bullets?", ref spawnBullets)) {
				root["spawn_bullets"] = spawnBullets;
			}
			
			var spawnBombs = root["spawn_bombs"].Bool(false);

			if (ImGui.Checkbox("Spawn Bombs?", ref spawnBombs)) {
				root["spawn_bombs"] = spawnBombs;
			}
			
			root.InputFloat("Radius Modifier", "radius", 1f);

			var setFuseTime = root["set_fuse"].Bool(false);

			if (ImGui.Checkbox("Set fuse?", ref setFuseTime)) {
				root["set_fuse"] = setFuseTime;
			}

			if (!setFuseTime) {
				return;
			}
			
			var fuseTime = root["fuse_time"].Number(1);

			if (ImGui.InputFloat("Fuse time", ref fuseTime)) {
				root["fuse_time"] = fuseTime;
			}
		}
	}
}
