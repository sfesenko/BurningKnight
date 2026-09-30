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
		public class LaserSwingAttack : SmartState<BurningKnight> {
			private Laser laser;
			private float vy;
			private float angle;
			
			public override void Init() {
				base.Init();

				angle = Self.AngleTo(Self.Target) - (Rnd.Chance() ? -1 : 1) * 1.2f;
				Self.WarnLaser(angle);
			}

			public override void Update(float dt) {
				base.Update(dt);

				if (laser == null) {
					if (T < 1f) {
						return;
					}

					if (Self.Raging) {
						Self.StartLasers();
					}
					
					laser = Laser.Make(Self, 0, 0, damage: 2, scale: 3, range: 64);
					laser.LifeTime = 10f;
					laser.Position = Self.Center;
					laser.Angle = angle;
					Self.GetComponent<AudioEmitterComponent>()!.EmitRandomizedPrefixed("item_laser", 4);
				}

				if (laser.Done) {
					Become<FightState>();
					return;
				}
				
				laser.Position = Self.Center;

				var aa = laser.Angle;
				var a = Self.AngleTo(Self.Target);
				
				vy += (float) MathUtils.ShortAngleDistance(aa, a) * dt * 4;

				laser.Angle += vy * dt * 0.5f;
			}
		}
	}
}
