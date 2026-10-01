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
		public class SpawnAttack : SmartState<BurningKnight> {
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
						LightRadius = 32f,
						Color = ProjectileColor.Orange
					}.Shoot(angle, 15f);

					builder.RemoveFlags(ProjectileFlags.BreakableByMelee, ProjectileFlags.Reflectable);

					var projectile = builder.Build();
					projectile!.Center += MathUtils.CreateVector(angle, 8);

					ProjectileCallbacks.AttachDeathCallback(projectile, (p, en, t) => {
						var x = (int) Math.Floor(p.CenterX / 16);
						var y = (int) Math.Floor(p.CenterY / 16);
						
						var mob = new WallCrawler();
						Self.Area!.Add(mob);
						mob.X = x * 16;
						mob.Y = y * 16 - 8;
						mob.GeneratePrefix();
						AnimationUtil.Poof(mob.Center, 1);
					});

					count--;

					if (count <= 0) {
						Become<FightState>();
					}
				}
			}
		}
	}
}
