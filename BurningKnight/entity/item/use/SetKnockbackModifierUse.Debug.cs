using BurningKnight.entity.component;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of SetKnockbackModifierUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class SetKnockbackModifierUse {
		public static void RenderDebug(JsonValue root) {
			var val = root["mod"].Number(0);

			if (ImGui.InputFloat("Modifier", ref val)) {
				root["mod"] = val;
			}
		}
	}
}
