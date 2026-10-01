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
		public class CaptureState : SmartState<BurningKnight> {
			public override void Init() {
				base.Init();

				GameContext.Current!.Camera.Targets!.Clear();
				GameContext.Current.Camera.Follow(Self, 0.3f);

				Timer.Add(() => { GameContext.Current.Camera.Follow(Self.captured!, 0.3f); }, 0.5f);
			}

			public override void Update(float dt) {
				base.Update(dt);

				var d = Self.DistanceTo(Self.captured!);

				if (d <= 8) {
					Audio.PlayMusic("Fatiga", true);

					// PREPARE TO DIE!
					Self!.captured!.GetComponent<DialogComponent>()!.StartAndClose(Self.captured.GetScream(), 5);
					Self.captured.GetComponent<AudioEmitterComponent>()!.EmitRandomized("mob_bk_capture");
					GameContext.Current!.Camera.Unfollow(Self);

					Become<HiddenState>();
					Self.captured.SelectAttack();
				} else if (d >= 400f &&
				           Self.GetComponent<RoomComponent>()!.Room != Self!.captured!.GetComponent<RoomComponent>()!.Room) {
					Become<TeleportState>();

					return;
				}

				var a = Self.AngleTo(Self.captured!);
				var force = 500f * dt;

				if (d <= 64f) {
					force *= 2;
				}

				Self.GetComponent<RectBodyComponent>()!.Velocity +=
					new Vector2((float) Math.Cos(a) * force, (float) Math.Sin(a) * force);
			}
		}
	}
}
