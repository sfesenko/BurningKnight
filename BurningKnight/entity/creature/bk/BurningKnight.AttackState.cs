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
		public class AttackState : SmartState<BurningKnight> {
			private int count;
			
			public override void Init() {
				base.Init();
				Self.raging = true;
				count = Math.Min(1, Self.timesRaged);
			}
			
			public override void Update(float dt) {
				base.Update(dt);
				Self.CheckForScourgeRageFree();

				if (Self.DistanceTo(Self.Target!) < 64f) {
					Self.Become<FlyAwayAttackingState>();
					return;
				}

				var r = Self!.Target.GetComponent<RoomComponent>()!.Room;

				if (r!.Type == RoomType.Shop || r.Type == RoomType.SubShop || r.Type == RoomType.OldMan) {
					Self.Become<ChaseState>();
					return;
				}

				if (T >= 1f) {
					Self.GetComponent<AudioEmitterComponent>()!.Emit("mob_bk_fire");

					var c = 1;

					if (Self.timesRaged > 2 && Self.timesRaged < 5) {
						c = 3;
					}

					var builder = new ProjectileBuilder(Self, "circle") {
						Scale = Rnd.Float(1f, 1f + Self.timesRaged * 0.1f),
						Color = ProjectileColor.BkRed,
						LightRadius = 32f
					};

					builder.AddFlags(ProjectileFlags.FlyOverWalls);
					builder.RemoveFlags(ProjectileFlags.Reflectable, ProjectileFlags.BreakableByMelee);

					for (var i = 0; i < c; i++) {
						var p = builder.Shoot(Self.AngleTo(Self.Target) + Rnd.Float(-0.4f, 0.4f) + (c == 1 ? 0 : (i - 1) * Math.PI * 0.2f),
							8 + Self.timesRaged * 0.3f).Build();

						p!.Center = Self.Center;
						p.Depth = Self.Depth;

						if (Self.timesRaged > 4) {
							ProjectileCallbacks.AttachUpdateCallback(p, TargetProjectileController.Make(Self.Target, 0.5f));
							p.T = 5f + Rnd.Float(1f);
						}
					}
					
					count--;
					T -= 0.25f + (Self.timesRaged - 1) * 0.1f;

					if (count <= 0) {
						Become<ChaseState>();
					}
				}
			}
		}
	}
}
