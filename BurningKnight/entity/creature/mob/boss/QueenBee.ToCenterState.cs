using System;
using BurningKnight.assets.achievements;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob.jungle;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.controller;
using BurningKnight.entity.projectile.pattern;
using BurningKnight.level;
using BurningKnight.level.entities;
using Lens.entity;
using Lens.util;
using Lens.util.camera;
using Lens.util.math;
using Lens.util.timer;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.creature.mob.boss {
	public partial class QueenBee {
		public class ToCenterState : SmartState<QueenBee> {
			public override void Update(float dt) {
				base.Update(dt);
				
				var r = Self.GetComponent<RoomComponent>()!.Room;
				var dx = r!.CenterX - Self.CenterX;
				var dy = r.CenterY - Self.CenterY;
				var d = MathUtils.Distance(dx, dy);

				if (d <= 32f) {
					if (Self.changingPhase) {
						Self.changingPhase = false;
						Become<RageState>();
					} else {
						Become<IdleState>();
					}
					
					return;
				}
				
				var s = (dt * 3600 / d) * (d > 48 ? 1 : d / 48);
				
				Self.GetComponent<RectBodyComponent>()!.Velocity += new Vector2(dx * s, dy * s);
			}
		}
	}
}
