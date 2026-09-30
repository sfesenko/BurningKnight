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
		public class RunState : SmartState<Player> {
			private float lastParticle = 0.25f;
			private uint lastFrame;
			
			public override void Update(float dt) {
				base.Update(dt);

				lastParticle -= dt;

				if (lastParticle <= 0) {
					lastParticle = 0.25f;
					
					var part = new ParticleEntity(Particles.Dust());
					
					part.Position = Self.Center;
					part.Particle.Scale = Rnd.Float(0.4f, 0.8f);
					Self.Area.Add(part);
				}

				if (!Self.HasFlight) {
					var anim = Self.GetComponent<PlayerGraphicsComponent>().Animation;

					if (anim.Frame != lastFrame) {
						lastFrame = anim.Frame;

						if (GameContext.Current.Level != null && (lastFrame == 2 || lastFrame == 6)) {
							var x = (int) (Self.CenterX / 16);
							var y = (int) (Self.Bottom / 16);

							if (!GameContext.Current.Level.IsInside(x, y)) {
								return;
							}

							var i = GameContext.Current.Level.ToIndex(x, y);
							var tile = GameContext.Current.Level.Get(i);
							var liquid = GameContext.Current.Level.Liquid[i];
							var room = Self.GetComponent<RoomComponent>().Room;

							Audio.PlaySfx(GameContext.Current.Level.Biome.GetStepSound(liquid == 0 ? tile : (Tile) liquid),
								room != null && room.Tagged[Tags.MustBeKilled].Count > 0 ? 0.18f : 0.25f);
						}
					}
				}
			}
		}
	}
}
