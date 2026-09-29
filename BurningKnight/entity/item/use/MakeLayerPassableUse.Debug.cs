using BurningKnight.entity.component;
using BurningKnight.entity.creature;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.door;
using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.level;
using BurningKnight.level.entities;
using BurningKnight.physics;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of MakeLayerPassableUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class MakeLayerPassableUse {
		public static void RenderDebug(JsonValue root) {
			var v = root["fp"].Bool(false);

			if (ImGui.Checkbox("For projectiles", ref v)) {
				root["fp"] = v;
			}
			
			v = root["fpl"].Bool(false);

			if (ImGui.Checkbox("For player", ref v)) {
				root["fpl"] = v;
			}


			ImGui.Separator();

			root.Checkbox("Projectiles", "p", false);
			
			v = root["ic"].Bool(false);

			if (ImGui.Checkbox("Ignore chasms", ref v)) {
				root["ic"] = v;
			}
			
			v = root["ip"].Bool(false);

			if (ImGui.Checkbox("Ignore props", ref v)) {
				root["ip"] = v;
			}
			
			v = root["iw"].Bool(false);

			if (ImGui.Checkbox("Ignore walls", ref v)) {
				root["iw"] = v;
			}
			
			v = root["im"].Bool(false);

			if (ImGui.Checkbox("Ignore mobs", ref v)) {
				root["im"] = v;
			}

			root.Checkbox("Ignore stones", "st", false);
			root.Checkbox("Break Projectiles", "bp", false);
		}
	}
}
