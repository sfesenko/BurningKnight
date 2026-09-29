using System;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.npc;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui.dialog;
using ImGuiNET;
using Lens;
using Lens.entity;
using Lens.util;
using Lens.util.file;
using Lens.util.math;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.entities {
	// The editor half of Stand; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Stand {
		public override void RenderImDebug() {
			base.RenderImDebug();

			if (ImGui.InputInt("Id", ref id)) {
				RemoveComponent<InteractableSliceComponent>();
				InsertGraphics();
			}
		}
	}
}
