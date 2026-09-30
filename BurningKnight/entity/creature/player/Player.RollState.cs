using BurningKnight.debug;
using System;
using System.Collections.Generic;
using BurningKnight.assets;
using BurningKnight.assets.achievements;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle;
using BurningKnight.assets.particle.controller;
using BurningKnight.assets.particle.custom;
using BurningKnight.assets.particle.renderer;
using BurningKnight.entity.bomb;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.bk;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.door;
using BurningKnight.entity.events;
using BurningKnight.entity.fx;
using BurningKnight.entity.item;
using BurningKnight.entity.item.stand;
using BurningKnight.entity.projectile;
using BurningKnight.entity.room;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.entities;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui;
using BurningKnight.ui.dialog;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.entity.component;
using Lens.entity.component.logic;
using Lens.graphics;
using Lens.graphics.gamerenderer;
using Lens.input;
using Lens.util;
using Lens.util.camera;
using Lens.util.file;
using Lens.util.math;
using Lens.util.timer;
using Lens.util.tween;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.creature.player {
	public partial class Player {
		public class RollState : EntityState {
			private const float RollTime = 0.25f;
			private const float RollForce = 1600f;
			
			private float lastParticle = 0.05f;
			private Vector2 direction;
			private bool wasUnhittable;
			
			public override void Init() {
				base.Init();

				var z = Self.GetComponent<ZComponent>();
				var start = z!.Z;

				Tween.To(start + 8, start, x => z.Z = x, 0.15f, Ease.QuadIn).OnEnd = () => {
					Tween.To(start, z.Z, x => z.Z = x, 0.15f, Ease.QuadIn);
				};
				
				Self.GetComponent<AudioEmitterComponent>()!.EmitRandomized("player_roll", 0.5f);
				var hp = Self.GetComponent<HealthComponent>();

				wasUnhittable = hp!.Unhittable;
				hp.Unhittable = true;

				var body = Self.GetComponent<RectBodyComponent>();
				var angle = body!.Acceleration.LengthSquared() > 0.1f 
					?	body.Acceleration.ToAngle()
					: (GameContext.Current!.Camera.ScreenToCamera(Input.Mouse.ScreenPosition) - Self.Center).ToAngle();

				direction = new Vector2((float) Math.Cos(angle) * RollForce, (float) Math.Sin(angle) * RollForce);
				
				for (var i = 0; i < 4; i++) {
					var part = new ParticleEntity(Particles.Dust());
						
					part.Position = Self.Center;
					part.Particle.Scale = Rnd.Float(0.4f, 0.8f);
					Self.Area!.Add(part);
				}
			}

			public override void Destroy() {
				base.Destroy();
				
				Self.GetComponent<HealthComponent>()!.Unhittable = wasUnhittable;
			}

			public override void Update(float dt) {
				base.Update(dt);

				if (T >= RollTime) {
					Become<PostRollState>();
					return;
				}
				
				var body = Self.GetComponent<RectBodyComponent>();
				body!.Velocity += direction * (RollTime - T * 0.5f);
				body.Position += body.Velocity * dt * 0.1f;

				lastParticle -= dt;

				if (lastParticle <= 0) {
					lastParticle = 0.1f;
					
					var part = new ParticleEntity(Particles.Dust());
						
					part.Position = Self.Center;
					part.Particle.Scale = Rnd.Float(0.4f, 0.8f);
					Self.Area!.Add(part);
				}
			}

			public void ChangeDirection() {
				direction *= -1;
			}
		}
	}
}
