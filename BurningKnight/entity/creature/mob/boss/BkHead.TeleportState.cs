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
		public class TeleportState : SmartState<BkHead> {
			public override void Init() {
				base.Init();

				Tween.To(0, 255, x => Self.GetComponent<BkGraphicsComponent>()!.Tint.A = (byte) x, 0.5f).OnEnd = () => {
					var tile = Self!.GetComponent<RoomComponent>()!.Room.GetRandomFreeTile() * 16;

					Self.BottomCenter = tile + new Vector2(8, 8); 

					Tween.To(255, 0, x => Self.GetComponent<BkGraphicsComponent>()!.Tint.A = (byte) x, 0.5f).OnEnd = () => {
						Become<IdleState>();
					};
				};
			}
		}
	}
}
