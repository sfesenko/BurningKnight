using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.pet;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	// The editor half of SpawnPetUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class SpawnPetUse {
		public static void RenderDebug(JsonNode root) {
			root.Checkbox("Only if has none", "oin", false);
			
			if (root.Checkbox("Random", "random", false)) {
				return;
			}
			
			if (!PetRegistry.Has(root.InputText("Pet", "pet", "", 128))) {
				ImGui.BulletText("Unknown pet!");
			}
		}
	}
}
