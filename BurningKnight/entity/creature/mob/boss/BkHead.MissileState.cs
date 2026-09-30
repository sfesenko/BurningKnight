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
		private class MissileState : SmartState<BkHead> {
			private const int SmallCount = 8;
			private const int InnerCount = 8;
			
			private float delay;
			private int count;

			public override void Update(float dt) {
				base.Update(dt);
				delay -= dt;

				if (delay <= 0) {
					if (count >= 5f) {
						Become<IdleState>();
						return;
					}
					
					delay = 3f;
					count++;

					Self.GetComponent<BkGraphicsComponent>()!.Animate();

					var m = new Missile(Self, Self.Target);
					Self.Area.Add(m);

					m.HurtOwner = false;

					ProjectileCallbacks.AttachDeathCallback(m, (p, e, t) => {
						var bb = new ProjectileBuilder(Self, "small");

						bb.RemoveFlags(ProjectileFlags.Reflectable, ProjectileFlags.BreakableByMelee);

						for (var i = 0; i < SmallCount; i++) {
							var an = (float) (((float) i) / SmallCount * Math.PI * 2);
						
							var pp = new ProjectilePattern(CircleProjectilePattern.Make(6.5f, 10 * (i % 2 == 0 ? 1 : -1))) {
								Position = p.Center
							};

							for (var j = 0; j < 5; j++) {
								pp.Add(bb.Build());
							}
				
							pp.Launch(an, 40);
							Self.Area.Add(pp);
						}

						var aa = Self.AngleTo(Self.Target);
						var bbb = new ProjectileBuilder(Self, "circle") {
							Color = ProjectileColor.Orange
						};

						bbb.RemoveFlags(ProjectileFlags.Reflectable, ProjectileFlags.BreakableByMelee);

						for (var i = 0; i < InnerCount; i++) {
							bbb.Scale = Rnd.Float(0.5f, 1f);
							var b = bbb.Shoot(aa + Rnd.Float(-0.3f, 0.3f), Rnd.Float(2, 12)).Build();
						
							b.Center = p.Center;
						}
					});
				}
			}
		}
	}
}
