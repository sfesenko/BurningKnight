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
		public class TeleportState : SmartState<BurningKnight> {
			private bool did;

			public override void Update(float dt) {
				base.Update(dt);

				if (did) {
					return;
				}

				did = true;
				
				Self.GetComponent<DialogComponent>()!.Close();
				var graphics = Self.GetComponent<BkGraphicsComponent>();

				Tween.To(0, graphics.Alpha, x => graphics.Alpha = x, 0.3f, Ease.QuadIn).OnEnd = () => {
					if (Self.Target != null) {
						Self.Center = Self.Target.Center + MathUtils.CreateVector(Rnd.AnglePI(), 64f);
					}

					Tween.To(1, graphics.Alpha, x => graphics.Alpha = x, 0.3f).OnEnd = () => {
						if (GameContext.Current.Run.Depth == 1 && GlobalSave.IsFalse("bk_who")) {
							Self.Become<CutsceneState>();
							return;
						}
						
						if (Self.sayNoRage) {
							Self.sayNoRage = false;
							Self.GetComponent<DialogComponent>()!.StartAndClose("bk_11", 5);
						}

						if (Self.Target != null) {
							Self.Become<FollowState>();
						} else {
							Self.Become<IdleState>();
						}
					};
				};
			}

			public override void Destroy() {
				base.Destroy();
				Self.CheckCapture();
			}
		}
	}
}
