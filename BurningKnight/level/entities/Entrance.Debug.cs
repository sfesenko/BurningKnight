using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.fx;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui.editor;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.util.file;
using Lens.physics;

namespace BurningKnight.level.entities {
	// The editor half of Entrance; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Entrance {
		public override void RenderImDebug() {
			ImGui.InputInt("To", ref To);
		}
	}
}
