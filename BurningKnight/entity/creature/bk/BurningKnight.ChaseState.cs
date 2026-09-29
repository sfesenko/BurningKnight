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
		public class ChaseState : SmartState<BurningKnight> {
			public override void Init() {
				base.Init();
				Self.raging = true;
			}
			
			public override void Update(float dt) {
				base.Update(dt);
				Self.CheckForScourgeRageFree();

				var d = Self.DistanceTo(Self.Target);
				var force = 300f * dt;

				if (d < 64f) {
					Self.Become<FlyAwayAttackingState>();
				} else if (d <= 128f) {
					var r = Self.Target.GetComponent<RoomComponent>().Room;

					if (r.Type == RoomType.Shop || r.Type == RoomType.SubShop || r.Type == RoomType.OldMan) {

					} else {
						Self.Become<AttackState>();
					}
				}

				var room = Self.Target.GetComponent<RoomComponent>().Room;

				if (Self.OnScreen && room != null && room.Type == RoomType.Regular &&
				    room.Tagged[Tags.MustBeKilled].Count > 0 && room.Contains(Self, 16f)) {
					var aa = Self.AngleTo(room);
					force = 400f * dt;

					Self.GetComponent<RectBodyComponent>().Velocity -=
						new Vector2((float) Math.Cos(aa) * force, (float) Math.Sin(aa) * force);

					return;
				}

				var a = Self.AngleTo(Self.Target);

				Self.GetComponent<RectBodyComponent>().Velocity +=
					new Vector2((float) Math.Cos(a) * force, (float) Math.Sin(a) * force);
			}
		}
	}
}
