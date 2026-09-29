using BurningKnight.assets.achievements;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.npc;
using BurningKnight.save;
using BurningKnight.state;
using ImGuiNET;
using Lens;
using Lens.entity;
using Lens.util.file;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.entities {
	// The editor half of StatDisplay; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class StatDisplay {
		public override void RenderImDebug() {
			base.RenderImDebug();

			if (board == null) {
				board = "";
			}

			ImGui.InputText("Board", ref board, 128);
			
			if (board == "challenge") {
				ImGui.InputInt("Challenge Id", ref challengeId);
			}
		}
	}
}
