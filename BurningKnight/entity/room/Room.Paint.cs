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
		public void ApplyToEachTile(Action<int, int> callback, int offset = 0) {
			var level = Context.Level;
			
			for (int y = MapY + offset; y < MapY + MapH - 1 - offset; y++) {
				for (int x = MapX + offset; x < MapX + MapW - offset; x++) {
					if (level!.IsInside(x, y)) {
						callback(x, y);
					}
				}
			}
		}
		public List<Point> GetFreeTiles(Func<int, int, bool>? filter = null) {
			var list = new List<Point>();

			for (var x = MapX + 1; x < MapX + MapW - 1; x++) {
				for (var y = MapY + 1; y < MapY + MapH - 1; y++) {
					if (Context.Level!.IsPassable(x, y) && (filter == null || filter(x, y))) {
						list.Add(new Point(x, y));
					}
				}
			}
			
			return list;
		}
		public Vector2 GetRandomFreeTile(Func<int, int, bool>? filter = null) {
			var tiles = GetFreeTiles(filter);

			if (tiles.Count == 0) {
				return Center;
			}

			var tile = tiles[Rnd.Int(tiles.Count)];
			return new Vector2(tile.X, tile.Y);
		}
		public Vector2 GetRandomFreeTileNearWall(Func<int, int, bool>? filter = null) {
			return GetRandomFreeTile((x, y) => {
				if (Context.Level!.CheckFor(x - 1, y, TileFlags.Passable)
				&& Context.Level!.CheckFor(x + 1, y, TileFlags.Passable)
				&& Context.Level!.CheckFor(x, y - 1, TileFlags.Passable)
				&& Context.Level!.CheckFor(x, y + 1, TileFlags.Passable)) {
					// No wall here :/
					return false;
				}
				
				return filter == null || filter(x, y);
			});
		}
		public Vector2 GetRandomWallFreeTile(Func<int, int, bool>? filter = null) {
			return GetRandomFreeTile((x, y) => {
				if (!Context.Level!.CheckFor(x - 1, y, TileFlags.Passable)
				    || !Context.Level!.CheckFor(x + 1, y, TileFlags.Passable)
				    || !Context.Level!.CheckFor(x, y - 1, TileFlags.Passable)
				    || !Context.Level!.CheckFor(x, y + 1, TileFlags.Passable)) {
					// Wall here :/
					return false;
				}
				
				return filter == null || filter(x, y);
			});
		}
		public void OpenHiddenDoors() {
			var level = Context.Level;
			
			foreach (var door in Doors) {
				var x = (int) Math.Floor(door.CenterX / 16);
				var y = (int) Math.Floor(door.CenterY / 16);
				var t = level!.Get(x, y);

				if (t == Tile.WallA || t == Tile.WallB) {
					var index = level.ToIndex(x, y);
			
					level.Set(index, Type == RoomType.OldMan ? Tile.EvilFloor : Tile.GrannyFloor);
					level.UpdateTile(x, y);
					level.ReCreateBodyChunk(x, y);
					level.LoadPassable();

					ExplosionMaker.LightUp(x * 16 + 8, y * 16 + 8);

					Level.Animate(Area, x, y);
				}
			}
		}
		public void CloseHiddenDoors() {
			var level = Context.Level;
			
			foreach (var door in Doors) {
				var x = (int) Math.Floor(door.CenterX / 16);
				var y = (int) Math.Floor(door.Bottom / 16);
				var t = level!.Get(x, y);

				if (level.Get(x, y).Matches(TileFlags.Passable)) {
					var index = level.ToIndex(x, y);
			
					level.Set(index, level.Biome is IceBiome ? Tile.WallB : Tile.WallA);
					level.UpdateTile(x, y);
					level.ReCreateBodyChunk(x, y);
					level.LoadPassable();

					Hide();

					Context.Camera!.Shake(10);
				}
			}
		}
		public void PaintTunnel(List<Door> Doors, Tile Floor, Rect? space = null, bool Bold = false, bool shift = true, bool randomRect = true) {
			if (Doors.Count == 0) {
				return;
			}

			var Level = Context.Level;
			var C = space;

			if (C == null) {
				var c = new Dot(MapX + MapW /2, MapY + MapH / 2);
				C = new Rect(c.X, c.Y, c.X, c.Y);
			}

			var minLeft = C.Left;
			var maxRight = C.Right;
			var minTop = C.Top;
			var maxBottom = C.Bottom;
			var Right = MapX + MapW - 1;
			var Bottom = MapY + MapH - 1;

			Painter.Clip = new Rect(MapX, MapY, MapX + MapW - 1, MapY + MapH - 1);

			foreach (var Door in Doors) {
				var dx = (int) Math.Floor(Door.CenterX / 16f);
				var dy = (int) Math.Floor(Door.CenterY / 16f);
				var Start = new Dot(dx, dy);
				Dot Mid;
				Dot End;

				if (shift) {
					if ((int) Start.X == MapX) {
						Start.X++;
					} else if ((int) Start.Y == MapY) {
						Start.Y++;
					} else if ((int) Start.X == Right) {
						Start.X--;
					} else if ((int) Start.Y == Bottom) {
						Start.Y--;
					}
				}

				int RightShift;
				int DownShift;

				if (Start.X < C.Left) {
					RightShift = (int) (C.Left - Start.X);
				} else if (Start.X > C.Right) {
					RightShift = (int) (C.Right - Start.X);
				} else {
					RightShift = 0;
				}

				if (Start.Y < C.Top) {
					DownShift = (int) (C.Top - Start.Y);
				} else if (Start.Y > C.Bottom) {
					DownShift = (int) (C.Bottom - Start.Y);
				} else {
					DownShift = 0;
				}

				if (dx == MapX || dx == Right) {
					Mid = new Dot(MathUtils.Clamp(MapX + 1, Right - 1, Start.X + RightShift), MathUtils.Clamp(MapY + 1, Bottom - 1, Start.Y));
					End = new Dot(MathUtils.Clamp(MapX + 1, Right - 1, Mid.X), MathUtils.Clamp(MapY + 1, Bottom - 1, Mid.Y + DownShift));
				} else {
					Mid = new Dot(MathUtils.Clamp(MapX + 1, Right - 1, Start.X), MathUtils.Clamp(MapY + 1, Bottom - 1, Start.Y + DownShift));
					End = new Dot(MathUtils.Clamp(MapX + 1, Right - 1, Mid.X + RightShift), MathUtils.Clamp(MapY + 1, Bottom - 1, Mid.Y));
				}

				Painter.DrawLine(Level, Start, Mid, Floor, Bold);
				Painter.DrawLine(Level, Mid, End, Floor, Bold);

				if (Rnd.Chance(10)) {
					Painter.Set(Level, End, Tiles.RandomFloor());
				}

				minLeft = Math.Min(minLeft, End.X);
				minTop = Math.Min(minTop, End.Y);
				maxRight = Math.Max(maxRight, End.X);
				maxBottom = Math.Max(maxBottom, End.Y);
			}

			if (randomRect && Rnd.Chance(20)) {
				if (Rnd.Chance()) {
					minLeft--;
				}
				
				if (Rnd.Chance()) {
					minTop--;
				}
				
				if (Rnd.Chance()) {
					maxRight++;
				}
				
				if (Rnd.Chance()) {
					maxBottom++;
				}
			}

			minLeft = MathUtils.Clamp(MapX + 1, Right - 1, minLeft);
			minTop = MathUtils.Clamp(MapY + 1, Bottom - 1, minTop);
			maxRight = MathUtils.Clamp(MapX + 1, Right - 1, maxRight);
			maxBottom = MathUtils.Clamp(MapY + 1, Bottom - 1, maxBottom);

			if (Rnd.Chance()) {
				Painter.Fill(Level, minLeft, minTop, maxRight - minLeft + 1, maxBottom - minTop + 1, Rnd.Chance() ? Floor : Tiles.RandomFloor());
			} else {
				Painter.Rect(Level, minLeft, minTop, maxRight - minLeft + 1, maxBottom - minTop + 1, Rnd.Chance() ? Floor : Tiles.RandomFloor());
			}
			
			Painter.Clip = null;
		}
		public bool ContainsTile(int x, int y, int d = 0) {
			return x >= MapX + d && x < MapX + MapW - d && y >= MapY + d - 1 && y < MapY + MapH - d;
		}
	}
}
