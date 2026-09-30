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
		public class LaserCageAttack : SmartState<BurningKnight> {
			private Laser[] lasers = new Laser[8];
			private Vector2 spot;

			private const float boxHalfSize = 32;

			private static Vector2[] laserOffsets = {
				new Vector2(-boxHalfSize, 0), 
				new Vector2(-boxHalfSize, 0), 
				new Vector2(boxHalfSize, 0),
				new Vector2(boxHalfSize, 0),
				new Vector2(0, -boxHalfSize),
				new Vector2(0, -boxHalfSize),
				new Vector2(0, boxHalfSize),
				new Vector2(0, boxHalfSize),
			};

			private static double[] laserAngles = {
				Math.PI * 0.5f,
				Math.PI * 1.5f,
				Math.PI * 0.5f,
				Math.PI * 1.5f,
				Math.PI,
				0,
				Math.PI,
				0
			};

			public override void Init() {
				base.Init();

				spot = Self.Center;

				for (var i = 0; i < 8; i++) {
					Self.WarnLaser((float) laserAngles[i], laserOffsets[i]);
				}
				
				Timer.Add(() => {
					Self.GetComponent<AudioEmitterComponent>()!.EmitRandomizedPrefixed("item_laser", 4);
					
					for (var i = 0; i < 8; i++) {
						var laser = Laser.Make(Self, 0, (float) laserAngles[i], damage: 2, scale: 3, range: 64);

						laser.LifeTime = 10f;
						laser.Position = spot + laserOffsets[i];

						lasers[i] = laser;
					}
				}, 1);
			}

			public override void Update(float dt) {
				base.Update(dt);

				if (T < 1f) {
					return;
				}
				
				spot = spot.Lerp(Self!.Target.Center, dt * 0.5f);

				for (var i = 0; i < 8; i++) {
					var laser = lasers[i];

					if (laser == null) {
						continue;
					}

					if (laser.Done) {
						foreach (var l in lasers) {
							if (l != null) {
								l.Done = true;
							}
						}
						
						Become<FightState>();
						return;
					}
					
					laser.Position = spot + laserOffsets[i];
				}
			}
		}
	}
}
