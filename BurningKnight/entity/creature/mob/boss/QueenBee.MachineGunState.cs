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
		public class MachineGunState : SmartState<QueenBee> {
			private float sinceLast;
			
			public override void Update(float dt) {
				base.Update(dt);

				sinceLast += dt;

				if (sinceLast >= 0.5f) {
					sinceLast = 0;
					var a = Self.AngleTo(Self.Target!);
					var second = Self.InThirdPhase;

					for (var i = 0; i < (second ? 1 : 3); i++) {
						var i1 = i;
						
						Timer.Add(() => {
							if (second) {
								var pp = new ProjectilePattern(CircleProjectilePattern.Make(9f, i1 % 2 == 0 ? -10 : 10)) {
									Position = Self.Center
								};

								var builder = new ProjectileBuilder(Self, "small") {
									LightRadius = 32f
								};

								Self.ModifyBuilder(builder);

								for (var j = 0; j < 4; j++) {
									builder.Slice = j % 2 == 0 ? "circle" : "small";
									builder.Color = j % 2 == 0 ? ProjectileColor.Orange : ProjectileColor.Red;

									pp.Add(builder.Build()!);
								}

								pp.Launch(a, 80);
								Self.Area!.Add(pp);
							} else {
								var builder = new ProjectileBuilder(Self, "circle") {
									Scale = Rnd.Float(0.8f, 1f),
									Color = Rnd.Chance() ? ProjectileColor.Yellow : ProjectileColor.Orange,
									LightRadius = 64
								};

								Self.ModifyBuilder(builder);

								builder.Shoot(a + Rnd.Float(-0.1f, 0.1f), 30f).Build();
							}
							
							Self.GetComponent<AudioEmitterComponent>()!.EmitRandomized("mob_bee_shot");
						}, i * 0.15f);
					}
				}
				
				var t = T + Math.PI * 0.5f;
				var r = Self.GetComponent<RoomComponent>()!.Room;

				var x = r!.CenterX + (float) Math.Cos(t) * (r.Width * 0.4f);
				var y = r.CenterY - r.Height * 0.2f + (float) Math.Sin(t * 2) * (r.Height * 0.2f);
				
				
				var dx = x - Self.CenterX;
				var dy = y - Self.CenterY;
				
				var s = (dt * 10);
				
				Self.CenterX += dx * s;
				Self.CenterY += dy * s;
				Self!.GraphicsComponent!.Flipped = dx < 0;
				

				if (t >= Math.PI * 4.5f) {
					Become<IdleState>();
				}
			}
		}
	}
}
