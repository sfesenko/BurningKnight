using BurningKnight.entity.component;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyHpUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyHpUse {
		public static void RenderDebug(JsonNode root) {
			root.InputInt("Amount", "amount");
			root.Checkbox("Set To Min", "to_min", false);
			root.Checkbox("Fully Heal", "to_max", false);
		}
	}
}
