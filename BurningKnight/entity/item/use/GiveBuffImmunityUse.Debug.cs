using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of GiveBuffImmunityUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class GiveBuffImmunityUse {
		public static void RenderDebug(JsonValue root) {
			GiveBuffUse.RenderDebug(root);
			ImGui.Separator();
			root.Checkbox("Give ice immunity", "ice", false);
			root.Checkbox("Pit immunity", "pit", false);
		}
	}
}
