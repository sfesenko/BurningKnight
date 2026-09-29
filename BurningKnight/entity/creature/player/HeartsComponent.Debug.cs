using System;
using BurningKnight.assets.achievements;
using BurningKnight.entity.component;
using BurningKnight.entity.events;
using ImGuiNET;
using Lens.entity;
using Lens.entity.component;
using Lens.util;
using Lens.util.file;

namespace BurningKnight.entity.creature.player {
	// The editor half of HeartsComponent; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class HeartsComponent {
		public override void RenderDebug() {
			base.RenderDebug();
			
			ImGui.Text($"Iron halfs: {shieldHalfs}");
			var v = (int) bombsMax;

			if (ImGui.InputInt("Bombs Max", ref v)) {
				bombsMax = (byte) v;
			}
			
			v = (int) bombs;

			if (ImGui.InputInt("Bombs", ref v)) {
				bombs = (byte) v;
			}
		}
	}
}
