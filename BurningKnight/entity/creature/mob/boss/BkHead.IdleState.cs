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
		private class IdleState : SmartState<BkHead> {
			public override void Init() {
				base.Init();
				Self.TouchDamage = 2;
			}

			public override void Update(float dt) {
				base.Update(dt);

				if (T <= 1f) {
					return;
				}

				switch (Self.counter) {
					case 0: {
						Become<LaserSnipeState>();
						break;
					}
					
					case 1: {
						Become<LaserSwingAttack>();
						break;
					}
					
					case 2: {
						Become<BulletHellState>();
						break;
					}
					
					case 3: {
						Become<SpawnAttack>();
						break;
					}
					
					case 4: {
						Become<MissileState>();
						break;
					}
				}

				Self.counter = (Self.counter + 1) % 5;
			}
		}
	}
}
