using System;
using BurningKnight.assets.achievements;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob.jungle;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.controller;
using BurningKnight.entity.projectile.pattern;
using BurningKnight.level;
using BurningKnight.level.entities;
using Lens.entity;
using Lens.util;
using Lens.util.camera;
using Lens.util.math;
using Lens.util.timer;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.creature.mob.boss {
	public partial class QueenBee {
		public class PrepareToPenetrateState : SmartState<QueenBee> {
			private float x;
			private bool locked;
			
			public override void Init() {
				base.Init();
				Self.penetrateCount++;

				var r = Self.GetComponent<RoomComponent>()!.Room;

				if (r!.CenterX > Self.CenterX) {
					x = r.X + 32;
				} else {
					x = r.Right - 32;
				}
			}

			public override void Update(float dt) {
				base.Update(dt);
				
				if (locked) {
					if (T >= 0.3f) {
						Become<PenetrateState>();
					}

					return;
				}

				var body = Self.GetComponent<RectBodyComponent>();
				body!.Velocity += new Vector2((x > Self.CenterX ? 1 : -1) * dt * 360, 0);

				var py = Self!.Target!.CenterY;
				var sy = Self.CenterY;
				
				body.Velocity += new Vector2(0, (py > sy ? 1 : -1) * dt * 360);

				if (Math.Abs(py - sy) < 6 && Math.Abs(x - Self.CenterX) < 16) {
					Self!.GraphicsComponent!.Flipped = !Self.GraphicsComponent.Flipped;
					locked = true;
					T = 0;
					body.Velocity = Vector2.Zero;
				}
			}
		}
	}
}
