using BurningKnight.assets.particle.custom;
using BurningKnight.entity.component;
using BurningKnight.state;
using ImGuiNET;
using Lens.assets;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyLuckUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyLuckUse {
		public static void RenderDebug(JsonValue root) {
			var val = root["amount"].Int(1);

			if (ImGui.InputInt("Amount", ref val)) {
				root["amount"] = val;
			}
		}
	}
}
