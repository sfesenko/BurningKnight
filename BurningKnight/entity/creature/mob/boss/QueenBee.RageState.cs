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
		public class RageState : SmartState<QueenBee> {
			private int count;
			
			public override void Init() {
				base.Init();
				GameContext.Current.Camera.Shake(10);
			}

			public override void Update(float dt) {
				base.Update(dt);

				if (T >= 0.1f) {
					count++;
					T = 0;

					if (count == 32) {
						Become<IdleState>();
						return;
					}
						
					var a = (count * 3 / 8f * Math.PI) + Rnd.Float(-0.5f, 0.5f);
					var builder = new ProjectileBuilder(Self, "circle") {
						Scale = Rnd.Float(1f, 2f),
						Color = Rnd.Chance() ? ProjectileColor.Yellow : ProjectileColor.Orange,
						LightRadius = 64
					};

					Self.ModifyBuilder(builder);

					builder.Shoot(a, 30f).Build();

					GameContext.Current.Camera.Shake(2);
					Self.GetComponent<AudioEmitterComponent>().EmitRandomized("mob_bee_swirly_shot");
				}
			}
		}
	}
}
