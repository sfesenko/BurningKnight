using System;
using BurningKnight.assets;
using BurningKnight.assets.achievements;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.bk;
using BurningKnight.entity.creature.mob.castle;
using BurningKnight.entity.creature.mob.desert;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.cutscene.entity;
using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.pattern;
using BurningKnight.state;
using BurningKnight.ui.dialog;
using BurningKnight.util;
using Lens.entity;
using Lens.graphics;
using Lens.util;
using Lens.util.camera;
using Lens.util.math;
using Lens.util.timer;
using Lens.util.tween;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.creature.mob.boss {
	public partial class BkHead {
		private class BulletHellState : SmartState<BkHead> {
			private float sinceLast;
		
			public override void Update(float dt) {
				base.Update(dt);
				sinceLast -= dt;

				if (sinceLast <= 0) {
					sinceLast = 0.5f; // Self.InFirstPhase ? 0.5f : 0.3f;
					var amount = 8;

					var builder = new ProjectileBuilder(Self, "small");

					builder.RemoveFlags(ProjectileFlags.Reflectable, ProjectileFlags.BreakableByMelee);

					for (var i = 0; i < amount; i++) {
						var a = Math.PI * 2 * ((float) i / amount) + (Math.Cos(Self.t * 2f) * Math.PI) * (i % 2 == 0 ? -1 : 1);
						var projectile = builder.Shoot(a, 8f + (float) Math.Cos(Self.t * 2f) * 3f);
						
						projectile.Color = ProjectileColor.DesertRainbow[Rnd.Int(ProjectileColor.DesertRainbow.Length)];
					}
					
					Self.GetComponent<BkGraphicsComponent>()!.Animate();
				}

				if (T >= 5f) {
					Become<IdleState>();
				}
			}
		}
	}
}
