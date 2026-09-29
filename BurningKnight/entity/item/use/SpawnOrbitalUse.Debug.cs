using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.orbital;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of SpawnOrbitalUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class SpawnOrbitalUse {
		public static void RenderDebug(JsonValue root) {
			root.Checkbox("Only if has none", "oin", false);
			
			if (root.Checkbox("Random", "random", false)) {
				return;
			}
			
			if (!OrbitalRegistry.Has(root.InputText("Orbital", "orbital", "", 128))) {
				ImGui.BulletText("Unknown orbital!");
			}
		}
	}
}
