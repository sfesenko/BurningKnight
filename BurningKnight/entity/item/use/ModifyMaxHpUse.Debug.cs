using System;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.util;
using ImGuiNET;
using Lens.assets;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyMaxHpUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyMaxHpUse {
		public static void RenderDebug(JsonValue root) {
			var val = root["amount"].Int(1);

			if (ImGui.InputInt("Amount", ref val)) {
				root["amount"] = val;
			}

			var giveHp = root["give_hp"].Bool(true);

			if (ImGui.Checkbox("Give health", ref giveHp)) {
				root["give_hp"] = giveHp;
			}

			root.Checkbox("Set", "set", false);
			root.Checkbox("Bomb", "bomb", false);
		}
	}
}
