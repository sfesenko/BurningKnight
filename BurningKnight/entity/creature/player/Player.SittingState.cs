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
		public class SittingState : EntityState {
			public override void Init() {
				base.Init();
				Self.GetComponent<PlayerGraphicsComponent>().Animate();
			}
			
			public override void Destroy() {
				base.Destroy();
				
				var pr = (PixelPerfectGameRenderer) Engine.Instance.StateRenderer;
				pr.EnableClip = false;
				Self.GetComponent<PlayerGraphicsComponent>().Animate();
			}
			
			public override void Update(float dt) {
				base.Update(dt);

				if (T >= 24f && Self.HasComponent<PlayerInputComponent>()) {
					Become<SleepingState>();
				}
			}
		}
	}
}
