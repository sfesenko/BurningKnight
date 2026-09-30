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
		public class LaserSwingAttack : SmartState<BkHead> {
			private class Data {
				public Laser Laser;
				public float Vy;
				public float Angle;
			}

			private Data[] data = new Data[2];
			
			public override void Init() {
				base.Init();
				var a = Self.AngleTo(Self.Target);

				for (var i = 0; i < 2; i++) {
					var angle = a - (i == 0 ? -1 : 1) * 1.2f;
					Self.WarnLaser(angle);

					data[i] = new Data();
					data[i].Angle = angle;
				}
			}

			public override void Update(float dt) {
				base.Update(dt);

				if (T < 0.4f) {
					return;
				}

				var made = false;
				
				for (var i = 0; i < 2; i++) {
					var info = data[i];
					
					if (info.Laser == null) {
						info.Laser = Laser.Make(Self, 0, 0, damage: 2, scale: 3, range: 64);
						info.Laser.LifeTime = 10f;
						info.Laser.Angle = info.Angle;

						if (!made) {
							made = true;
							Self.GetComponent<AudioEmitterComponent>()!.EmitRandomizedPrefixed("item_laser", 4);
						}

						Log.Info("made laser");
					} else if (info.Laser.Done) {
						Become<IdleState>();
						return;
					}

					info.Laser.Position = Self.Center;

					var aa = info.Laser.Angle;
					var a = Self.AngleTo(Self.Target);

					info.Vy += (float) MathUtils.ShortAngleDistance(aa, a) * dt * 4;
					info.Laser.Angle += info.Vy * dt * 0.5f;
				}
			}
		}
	}
}
