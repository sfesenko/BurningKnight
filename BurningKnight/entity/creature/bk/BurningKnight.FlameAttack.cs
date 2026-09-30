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
		public class FlameAttack : SmartState<BurningKnight> {
			private float r;
			private List<int> last = new List<int>();
			
			public override void Init() {
				base.Init();
				
				var graphics = Self.GetComponent<BkGraphicsComponent>();
				Self.GetComponent<HealthComponent>()!.Unhittable = true;
				Tween.To(0, graphics!.Alpha, x => graphics.Alpha = x, 0.3f);
				Self.TouchDamage = 0;
			}

			public override void Destroy() {
				base.Destroy();
				
				var graphics = Self.GetComponent<BkGraphicsComponent>();
				Self.GetComponent<HealthComponent>()!.Unhittable = false;
				Self.TouchDamage = 2;
				Tween.To(1, graphics!.Alpha, x => graphics.Alpha = x, 0.3f);

				foreach (var l in last) {
					GameContext.Current!.Level.SetFlag(l, Flag.Burning, false);
				}
			}

			public override void Update(float dt) {
				base.Update(dt);

				if (T >= 10f) {
					Become<FightState>();
					return;
				}
				
				r = Math.Min(1.5f, r + dt * 60);
				
				var x = (int) Math.Floor(Self.CenterX / 16);
				var y = (int) Math.Floor(Self.CenterY / 16);

				for (var xx = (int) -r; xx <= r; xx++) {
					for (var yy = (int) -r; yy <= r; yy++) {
						if (Math.Sqrt(xx * xx + yy * yy) <= r) {
							var i = GameContext.Current!.Level.ToIndex(x + xx, y + yy);

							if (!GameContext.Current.Level.CheckFlag(i, Flag.Burning)) {
								GameContext.Current.Level.SetFlag(i, Flag.Burning, true);
								last.Add(i);

								Timer.Add(() => {
									last.Remove(i);
									GameContext.Current.Level.SetFlag(i, Flag.Burning, false);
								}, Rnd.Float(1.5f, 2.5f));
							}
						}
					}
				}
				
				var force = 250f * dt;
				var a = Self.AngleTo(Self.Target);

				Self.GetComponent<RectBodyComponent>()!.Velocity += new Vector2((float) Math.Cos(a) * force, (float) Math.Sin(a) * force);
			}
		}
	}
}
