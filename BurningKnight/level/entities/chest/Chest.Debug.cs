using System;
using BurningKnight.assets;
using BurningKnight.entity.component;
using BurningKnight.entity.creature;
using BurningKnight.physics;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.util.file;
using Lens.util.tween;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.entities.chest {
	// The editor half of Chest; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Chest {
		public override void RenderImDebug() {
			base.RenderImDebug();

			ImGui.Checkbox("Empty", ref Empty);
			ImGui.Checkbox("Can open", ref CanOpen);
		}
	}
}
