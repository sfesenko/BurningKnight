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
		public class FightState : SmartState<BurningKnight> {
			public override void Update(float dt) {
				base.Update(dt);

				if (T >= 1f) {
					switch (Self.count) {
						case 0: {
							Become<LaserSwingAttack>();
							break;
						}
						
						case 1: {
							Become<SpawnAttack>();
							break;
						}

						case 2: {
							Become<LaserRotateAttack>();
							break;
						}
						
						case 3: {
							Become<FlameAttack>();
							break;
						}
						
						case 4: {
							Become<SwordAttackState>();
							break;
						}
						
						case 5: {
							Become<SkullAttack>();
							break;
						}
						
						case 6: {
							Become<LaserCageAttack>();
							break;
						}
					}
					
					Self.count = (Self.count + 1) % (Self.Raging ? 7 : 6);
				}
			}
		}
	}
}
