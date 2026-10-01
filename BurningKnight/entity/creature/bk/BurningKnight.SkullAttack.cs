using System;
using System.Collections.Generic;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob.boss;
using BurningKnight.entity.creature.mob.castle;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.item;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.controller;
using BurningKnight.entity.projectile.pattern;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.rooms;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui;
using BurningKnight.ui.dialog;
using BurningKnight.util;
using Lens;
using Lens.entity;
using Lens.entity.component.logic;
using Lens.util;
using Lens.util.camera;
using Lens.util.file;
using Lens.util.math;
using Lens.util.timer;
using Lens.util.tween;
using Lens.physics;
using Color = Microsoft.Xna.Framework.Color;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace BurningKnight.entity.creature.bk {
	public partial class BurningKnight {
		public class SkullAttack : SmartState<BurningKnight> {
			private int count;
			private bool explode;

			public override void Init() {
				base.Init();
				explode = Rnd.Chance();
			}

			public override void Update(float dt) {
				base.Update(dt);

				if ((count + 1) * (Self.Raging ? 0.7f : 1f) <= T) {
					count++;

					if (Self.Target == null || Self.Died) {
						return;
					}
					
					var a = Self.GetComponent<BkGraphicsComponent>();
					Self.GetComponent<AudioEmitterComponent>()!.EmitRandomized("mob_oldking_shoot");

					Tween.To(1.8f, a!.Scale.X, x => a.Scale.X = x, 0.2f);
					Tween.To(0.2f, a.Scale.Y, x => a.Scale.Y = x, 0.2f).OnEnd = () => {

						Tween.To(1, a.Scale.X, x => a.Scale.X = x, 0.3f);
						Tween.To(1, a.Scale.Y, x => a.Scale.Y = x, 0.3f);

						if (Self.Target == null || Self.Died) {
							return;
						}

						var builder = new ProjectileBuilder(Self, explode ? "skull" : "skup") {
							Range = 5
						}.Shoot(Rnd.AnglePI(), explode ? Rnd.Float(5, 12) : 14);

						builder.RemoveFlags(ProjectileFlags.Reflectable, ProjectileFlags.BreakableByMelee);

						var skull = builder.Build();
						ProjectileCallbacks.AttachUpdateCallback(skull!, TargetProjectileController.Make(Self.Target, 0.5f));

						if (explode) {
							/*skull.NearDeath += p => {
								var c = new AudioEmitterComponent {
									DestroySounds = false
								};
								
								p.AddComponent(c);
								c.Emit("mob_oldking_explode");
							};*/
						
							ProjectileCallbacks.AttachDeathCallback(skull!, (p, e, t) => {
								if (!t) {
									return;
								}

								var b = new ProjectileBuilder(Self, "small");

								b.RemoveFlags(ProjectileFlags.Reflectable, ProjectileFlags.BreakableByMelee);
						
								for (var i = 0; i < 16; i++) {
									var bullet = b.Shoot(((float) i) / 8 * (float) Math.PI, (i % 2 == 0 ? 2 : 1) * 4 + 3).Build();
									bullet!.Center = p.Center;
								}
							});
						}

						skull!.GetComponent<ProjectileGraphicsComponent>()!.IgnoreRotation = true;
						
						if (count == (Self.Raging ? 6 : 4)) {
							Self.Become<FightState>();
						}
					};
				}
			}
		}
	}
}
