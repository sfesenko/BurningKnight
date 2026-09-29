using System;
using BurningKnight.entity.component;
using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.level;
using BurningKnight.level.rooms;
using BurningKnight.physics;
using BurningKnight.state;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.util;
using Lens.util.file;
using Lens.util.tween;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.room.controllable.turret {
	// The editor half of Turret; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Turret {
		public override void RenderImDebug() {
			base.RenderImDebug();
			var u = (int) StartingAngle;

			if (ImGui.InputInt("Starting angle", ref u)) {
				Angle = StartingAngle = (uint) u % 8;
			}

			ImGui.InputFloat("Before next", ref beforeNextBullet);
			ImGui.InputFloat("Speed", ref Speed);
		}
	}
}
