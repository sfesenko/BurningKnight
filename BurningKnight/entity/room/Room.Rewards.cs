using System;
using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.bomb;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.door;
using BurningKnight.entity.events;
using BurningKnight.entity.item;
using BurningKnight.entity.projectile;
using BurningKnight.entity.room.controllable;
using BurningKnight.entity.room.controllable.platform;
using BurningKnight.entity.room.controller;
using BurningKnight.entity.room.input;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.entities.chest;
using BurningKnight.level.rooms;
using BurningKnight.level.rooms.granny;
using BurningKnight.level.rooms.oldman;
using BurningKnight.level.tile;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using BurningKnight.util.geometry;
using Lens;
using Lens.entity;
using Lens.graphics;
using Lens.util;
using Lens.util.camera;
using Lens.util.file;
using Lens.util.math;
using Lens.util.timer;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace BurningKnight.entity.room {
	public partial class Room {
		private static string[] rewards = {
			// "bk:copper_coin",
			"bk:key",
			"bk:key",
			"bk:bomb",
			"bk:bomb",
			"bk:troll_bomb",
			// "bk:heart",
			// "bk:heart",
			"bk:pouch",
			"bk:copper_coin"
		};
		private Entity CreateReward() {
			if (Rnd.Chance(LevelSave.ChestRewardChance)) {
				return ChestRegistry.PlaceRandom(Vector2.Zero, Area);
			}

			var id = rewards[Rnd.Int(rewards.Length)];

			if (id == "bk:troll_bomb") {
				var bomb = new Bomb(null);
				Area.Add(bomb);
				
				return bomb;
			}
			
			return Items.CreateAndAdd(id, Area);
		}
		private void SpawnReward() {
			if (Context.Run.Depth < 1 || Type != RoomType.Regular || Rnd.Chance(40 - Context.Run.Luck)) {
				return;
			}

			foreach (var e in Tagged[Tags.LevelSave]) {
				if (e is MovingPlatform) {
					// To avoid it getting stuck
					return;
				}
			}
			
			var where = new Dot(MapX + MapW / 2, MapY + MapH / 2);
			
			for (var x = -1; x < 2; x++) {
				for (var y = -1; y < 2; y++) {
					var x1 = x;
					var y1 = y;

					Timer.Add(() => {
						var part = new TileParticle();

						part.Top = Context.Level!.Tileset.FloorD[0];
						part.TopTarget = Context.Level!.Tileset.WallTopADecor;
						part.Side = Context.Level!.Tileset.FloorSidesD[0];
						part.Sides = Context.Level!.Tileset.WallSidesA[2];
						part.Tile = Tile.FloorD;

						part.X = (where.X + x1) * 16;
						part.Y = (where.Y + y1) * 16 + 8;
						part.Target.X = (where.X + x1) * 16;
						part.Target.Y = (where.Y + y1) * 16 + 8;
						part.TargetZ = -8f;

						Area.Add(part);
					}, Rnd.Float(1f));
				}
			}
			
			Timer.Add(() => {
				var reward = CreateReward();
				reward.BottomCenter = where * 16 + new Vector2(8, 24);
				AnimationUtil.Poof(reward.Center);
			}, 1.5f);
		}
	}
}
