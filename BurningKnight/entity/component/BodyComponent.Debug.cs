using System;
using BurningKnight.entity.events;
using BurningKnight.physics;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.entity.component;
using Lens.physics;
using Lens.util;
using Lens.util.file;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.component {
	// The editor half of BodyComponent; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class BodyComponent {
		public override void RenderDebug() {
			ImGui.DragFloat("Knockback modifier", ref KnockbackModifier);
			
			if (Body == null) {
				ImGui.BulletText("Body is null");
			} else {
				var vel = Body.LinearVelocity;
				var v = new System.Numerics.Vector2(vel.X, vel.Y);

				if (ImGui.DragFloat2("Velocity", ref v)) {
					Body.LinearVelocity = vel;
				}
			
				if (ImGui.Button("Sync body")) {
					PositionChangedListener();
				}
			}
		}
	}
}
