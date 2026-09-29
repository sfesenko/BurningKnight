using BurningKnight.assets.achievements;
using BurningKnight.entity;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.fx;
using BurningKnight.level.entities.exit;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui.editor;
using BurningKnight.util;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.util.file;
using Lens.physics;

namespace BurningKnight.level.entities {
	// The editor half of Exit; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Exit {
		public override void RenderImDebug() {
			ImGui.InputInt("To", ref To);
		}
	}
}
