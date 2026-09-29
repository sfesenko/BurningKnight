using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui.editor;
using ImGuiNET;
using Lens;
using Lens.entity;
using Lens.graphics;
using Lens.util;
using Lens.util.file;
using Lens.physics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace BurningKnight.entity.door {
	// The editor half of TeleportTrigger; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class TeleportTrigger {
		public override void RenderImDebug() {
			var v = (int) depth;

			if (id == null) {
				id = "";
			}
			
			ImGui.InputText("Id", ref id, 128);
			
			if (ImGui.InputInt("To depth", ref v)) {
				depth = (sbyte) v;
			}
			
			if (v == 0) {
				if (toId == null) {
					toId = "";
				}
				
				ImGui.InputText("To Id", ref toId, 128);
			}

			ImGui.Separator();

			var x = (int) X;
			var y = (int) Y;

			if (ImGui.InputInt("X", ref x)) {
				X = x;
			}

			if (ImGui.InputInt("Y", ref y)) {
				Y = y;
			}
			
			ImGui.InputFloat("Width", ref Width);
			ImGui.InputFloat("Height", ref Height);
		}
	}
}
