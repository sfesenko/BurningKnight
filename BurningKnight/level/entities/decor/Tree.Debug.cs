using System.Collections.Generic;
using BurningKnight.entity;
using BurningKnight.entity.component;
using BurningKnight.entity.creature;
using BurningKnight.entity.creature.mob.jungle;
using BurningKnight.entity.events;
using BurningKnight.entity.room.controllable.spikes;
using BurningKnight.level.entities.plant;
using ImGuiNET;
using Lens.entity;
using Lens.util.file;
using Lens.util.math;

namespace BurningKnight.level.entities.decor {
	// The editor half of Tree; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Tree {
		public override void RenderImDebug() {
			base.RenderImDebug();
			var v = (int) type;

			if (ImGui.InputInt("Id", ref v)) {
				type = (byte) v;
				UpdateSprite();
			}
		}
	}
}
