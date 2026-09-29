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
		public class SpamBeesState : SmartState<QueenBee> {
			private float sinceLast;
			private int count;
			
			public override void Update(float dt) {
				base.Update(dt);

				if (count < 5) {
					sinceLast += dt;

					if (sinceLast >= 1f - Self.Phase * 0.25f) {
						sinceLast = 0;
						count++;

						var bee = BeeHive.GenerateBee();
						Self.Area.Add(bee);
						bee.Center = Self.Center;
						Self.GetComponent<MobAnimationComponent>().Animate();
					}

					T = 0;
				}

				if (T >= 1) {
					Become<IdleState>();
				}
			}
		}
	}
}
