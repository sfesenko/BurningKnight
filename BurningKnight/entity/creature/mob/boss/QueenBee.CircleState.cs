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
		public class CircleState : SmartState<QueenBee> {
			private float sinceLast;
			
			public override void Update(float dt) {
				base.Update(dt);

				sinceLast += dt;
				var t = T * (Self.InSecondPhase ? 1.5f : 2f);

				if (sinceLast >= 0.15f) {
					sinceLast = 0;

					var builder = new ProjectileBuilder(Self, "circle") {
						Scale = Rnd.Float(0.5f, 1f),
						LightRadius = 64
					};

					Self.ModifyBuilder(builder);

					builder.Shoot(t + Math.PI + Rnd.Float(-0.1f, 0.1f), Rnd.Float(4f, 10f)).Build();
				}
				
				var r = Self.GetComponent<RoomComponent>()!.Room;

				var x = Self!.Target!.CenterX + (float) Math.Cos(t) * (r!.Width * 0.3f);
				var y = Self.Target.CenterY + (float) Math.Sin(t) * (r.Height * 0.3f);
				
				var dx = x - Self.CenterX;
				var dy = y - Self.CenterY;
				
				var s = (dt * 4);
				
				Self.CenterX += dx * s;
				Self.CenterY += dy * s;
				Self!.GraphicsComponent!.Flipped = dx < 0;

				if (t >= Math.PI * 6f) {
					Become<IdleState>();
				}
			}
		}
	}
}
