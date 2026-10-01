using System;
using BurningKnight.entity.component;
using BurningKnight.entity.item.util;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.input;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.item.use {
	// The editor half of MeleeArcUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class MeleeArcUse {
		public static void RenderDebug(JsonNode root) {
			root.InputFloat("Damage", "damage", 1);
			root.InputInt("Width", "w", 8);
			root.InputInt("Height", "h", 24);

			root.InputFloat("Life time", "time", 0.2f);
			root.InputFloat("Angle", "angle", 0);
			root.InputFloat("Knockback", "knockback", 0);

			ImGui.Separator();

			root.InputText("Hit Sound", "hs", "item_sword_hit");
			root.InputText("Attack Sound", "as", "item_sword_attack");
		}
	}
}
