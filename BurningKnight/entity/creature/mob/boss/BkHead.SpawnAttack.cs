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
		public class SpawnAttack : SmartState<BkHead> {
			private int count;
			private float delay;

			public override void Init() {
				base.Init();
				count = Rnd.Int(4, 10);
			}

			public override void Update(float dt) {
				base.Update(dt);
				delay -= dt;

				if (delay <= 0) {
					delay = 0.3f;
					Self.GetComponent<BkGraphicsComponent>()!.Animate();

					var angle = Rnd.AnglePI() * 0.5f + count * (float) Math.PI;
					var builder = new ProjectileBuilder(Self, "big") {
						Color = ProjectileColor.Orange,
						LightRadius = 32f
					};

					builder.RemoveFlags(ProjectileFlags.BreakableByMelee, ProjectileFlags.Reflectable, ProjectileFlags.BreakableByMelee);

					var projectile = builder.Shoot(angle, 15f).Build();
					projectile.Center += MathUtils.CreateVector(angle, 8);


					ProjectileCallbacks.AttachDeathCallback(projectile, (p, en, t) => {
						var x = (int) Math.Floor(p.CenterX / 16);
						var y = (int) Math.Floor(p.CenterY / 16);
						
						var mob = Rnd.Chance(40) ? (Mob) new DesertBulletSlime() : new Gunner();
						Self.Area!.Add(mob);
						mob.X = x * 16;
						mob.Y = y * 16 - 8;
						mob.GeneratePrefix();
						AnimationUtil.Poof(mob.Center, 1);
					});

					count--;

					if (count <= 0) {
						Become<IdleState>();
					}
				}
			}
		}
	}
}
