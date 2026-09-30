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
		public class PenetrateState : SmartState<QueenBee> {
			private float x;
			private float sign;
			private float delay = 1;
			private float lastBullet;
			
			public override void Init() {
				base.Init();

				var r = Self.GetComponent<RoomComponent>()!.Room;

				if (r!.CenterX < Self.CenterX) {
					x = r.X + 32;
					sign = -1;
				} else {
					x = r.Right - 32;
					sign = 1;
				}

				GameContext.Current.Camera.Shake(10);
			}

			public override void Destroy() {
				base.Destroy();

				var v = Self.penetrateCount;
				var max = 2 + Self.Phase * 2;

				if (v == max) {
					Self.penetrateCount = 0;
				}
			}

			public override void Update(float dt) {
				base.Update(dt);
				
				var body = Self.GetComponent<RectBodyComponent>();

				if ((Self.CenterX - x) * sign >= -16) {
					body!.Velocity -= body.Velocity * (dt * 2);
					delay -= dt;

					if (delay <= 0) {
						Become<IdleState>();
					}
					
					return;
				}

				if (!Self.InFirstPhase) {
					lastBullet -= dt;

					if (lastBullet <= 0) {
						lastBullet = 0.2f;
						
						var a = (sign < 0 ? Math.PI : 0) + Rnd.Float(-1f, 1f);
						var builder = new ProjectileBuilder(Self, "circle") {
							Bounce = 5,
							Color = ProjectileColor.Orange,
							LightRadius = 64,
							Scale = Rnd.Float(0.6f, 2f)
						};

						Self.ModifyBuilder(builder);

						var p = builder.Shoot(a, Rnd.Float(3f, 10f)).Build();

						ProjectileCallbacks.AttachUpdateCallback(p, SlowdownProjectileController.Make(0.25f));
						Self.GetComponent<AudioEmitterComponent>()!.EmitRandomized("mob_bee_shot");
					}
				}

				Self.X += sign * dt * 360;
				body!.Velocity += new Vector2(sign * dt * 3600, 0);
			}
		}
	}
}
