using System;
using System.Collections.Generic;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.room;
using BurningKnight.level.floors;
using BurningKnight.level.rooms.boss;
using BurningKnight.level.rooms.challenge;
using BurningKnight.level.rooms.connection;
using BurningKnight.level.rooms.darkmarket;
using BurningKnight.level.rooms.entrance;
using BurningKnight.level.rooms.granny;
using BurningKnight.level.rooms.oldman;
using BurningKnight.level.rooms.payed;
using BurningKnight.level.rooms.scourged;
using BurningKnight.level.rooms.secret;
using BurningKnight.level.rooms.shop;
using BurningKnight.level.rooms.shop.sub;
using BurningKnight.level.rooms.special;
using BurningKnight.level.rooms.spiked;
using BurningKnight.level.rooms.trap;
using BurningKnight.level.rooms.treasure;
using BurningKnight.level.tile;
using BurningKnight.level.walls;
using BurningKnight.state;
using BurningKnight.util;
using BurningKnight.util.geometry;
using Lens.util;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.rooms {
	public partial class RoomDef {
		public void PaintTunnel(Level Level, Tile Floor, Rect? space = null, bool Bold = false, bool shift = true, bool randomRect = true, RoomDef? defTo = null, DoorPlaceholder? to = null) {
			if (Connected.Count == 0) {
				Log.Error("Invalid connection room");

				return;
			}

			var C = space ?? GetConnectionSpace();
			var minLeft = C.Left;
			var maxRight = C.Right;
			var minTop = C.Top;
			var maxBottom = C.Bottom;

			var doors = to == null
				? Connected
				: new Dictionary<RoomDef, DoorPlaceholder?>() {
					{ defTo!, to }
				};

			foreach (var pair in doors) {
				if (pair.Key is GrannyRoom || pair.Key is OldManRoom) {
					continue;
				}
				
				var Door = pair.Value;
				var Start = new Dot(Door.X, Door.Y);
				Dot Mid;
				Dot End;

				if (shift) {
					if ((int) Start.X == Left) {
						Start.X++;
					} else if ((int) Start.Y == Top) {
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

				if (Door.X == Left || Door.X == Right) {
					Mid = new Dot(MathUtils.Clamp(Left + 1, Right - 1, Start.X + RightShift), MathUtils.Clamp(Top + 1, Bottom - 1, Start.Y));
					End = new Dot(MathUtils.Clamp(Left + 1, Right - 1, Mid.X), MathUtils.Clamp(Top + 1, Bottom - 1, Mid.Y + DownShift));
				} else {
					Mid = new Dot(MathUtils.Clamp(Left + 1, Right - 1, Start.X), MathUtils.Clamp(Top + 1, Bottom - 1, Start.Y + DownShift));
					End = new Dot(MathUtils.Clamp(Left + 1, Right - 1, Mid.X + RightShift), MathUtils.Clamp(Top + 1, Bottom - 1, Mid.Y));
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

			minLeft = MathUtils.Clamp(Left + 1, Right - 1, minLeft);
			minTop = MathUtils.Clamp(Top + 1, Bottom - 1, minTop);
			maxRight = MathUtils.Clamp(Left + 1, Right - 1, maxRight);
			maxBottom = MathUtils.Clamp(Top + 1, Bottom - 1, maxBottom);

			if (Rnd.Chance()) {
				Painter.Fill(Level, minLeft, minTop, maxRight - minLeft + 1, maxBottom - minTop + 1, Rnd.Chance() ? Floor : Tiles.RandomFloorOrSpike());
			} else {
				Painter.Rect(Level, minLeft, minTop, maxRight - minLeft + 1, maxBottom - minTop + 1, Rnd.Chance() ? Floor : Tiles.RandomFloorOrSpike());
			}
		}
	}
}
