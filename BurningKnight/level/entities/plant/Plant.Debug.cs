using BurningKnight.assets.lighting;
using BurningKnight.entity.component;
using BurningKnight.level.biome;
using BurningKnight.save;
using BurningKnight.state;
using ImGuiNET;
using Lens.util.file;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.entities.plant {
	// The editor half of Plant; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Plant {
		public override void RenderImDebug() {
			base.RenderImDebug();
			var v = (int) Variant;

			if (ImGui.InputInt("Id", ref v)) {
				Variant = (byte) v;
				RemoveComponent<PlantGraphicsComponent>();
			}
		}
	}
}
