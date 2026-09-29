using BurningKnight.entity.component;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	// The editor half of GivePhaseUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class GivePhaseUse {
		public static void RenderDebug(JsonValue root) {
			var val = root["amount"].Int(1);

			if (ImGui.InputInt("Amount", ref val)) {
				root["amount"] = val;
			}

			root.Checkbox("Broken", "broken", false);
		}
	}
}
