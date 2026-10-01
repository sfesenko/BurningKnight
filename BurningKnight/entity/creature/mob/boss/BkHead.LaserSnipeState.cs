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
		private class LaserSnipeState : SmartState<BkHead> {
			private float delay;
			private int count;
			
			public override void Update(float dt) {
				base.Update(dt);
				delay -= dt;
				
				if (delay <= 0) {
					if (count >= 3) {
						Become<IdleState>();
						return;
					}
					
					delay = 0.5f;
					var a = Self.AngleTo(Self.Target!);
					Self.WarnLaser(a);

					Timer.Add(() => {
						Self.GetComponent<AudioEmitterComponent>()!.EmitRandomizedPrefixed("item_laser", 4);
						var laser = Laser.Make(Self, a, 0, damage: 2, scale: 3, range: 64);
						laser.LifeTime = 1f;
						laser.Position = Self.Center;
						Self.GetComponent<BkGraphicsComponent>()!.Animate();
					}, 0.2f);

					count++;
				}
			}
		}
	}
}
