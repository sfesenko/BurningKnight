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
using Lens.util.camera;
using Lens.util.file;
using Lens.physics;

namespace BurningKnight.level.entities {
	// The editor half of HiddenExit; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class HiddenExit {
		public override void RenderImDebug() {
			base.RenderImDebug();

			if (id == null) {
				id = "";
			}

			ImGui.InputText("Id", ref id, 64);
		}
	}
}
