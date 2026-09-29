using BurningKnight.entity.events;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of MakeProjectilesBounceUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class MakeProjectilesBounceUse {
		public static void RenderDebug(JsonValue root) {
			var count = root["count"].Int(1);

			if (ImGui.InputInt("Count", ref count)) {
				root["count"] = count;
			}
		}
	}
}
