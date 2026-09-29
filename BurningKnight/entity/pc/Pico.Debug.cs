using BurningKnight.assets.input;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui.editor;
using ImGuiNET;
using Lens;
using Lens.entity;
using Lens.graphics;
using Lens.input;
using Lens.util.camera;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.pc {
	// The editor half of Pico; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Pico {
		public override void RenderImDebug() {
			base.RenderImDebug();
			ImGui.InputText("Cart", ref cart, 64);
			ImGui.SameLine();
			
			if (ImGui.Button("Load")) {
				LoadCart();
			}
		}
	}
}
