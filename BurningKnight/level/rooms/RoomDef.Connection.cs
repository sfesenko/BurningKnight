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
		public int GetCurrentConnections(Connection Direction) {
			if (Direction == Connection.All) {
				return Connected.Count;
			}

			var Total = 0;

			foreach (var R in Connected.Keys) {
				var I = Intersect(R);

				if (Direction == Connection.Left && I.GetWidth() == 0 && I.Left == Left) {
					Total++;
				} else if (Direction == Connection.Top && I.GetHeight() == 0 && I.Top == Top) {
					Total++;
				} else if (Direction == Connection.Right && I.GetWidth() == 0 && I.Right == Right) {
					Total++;
				} else if (Direction == Connection.Bottom && I.GetHeight() == 0 && I.Bottom == Bottom) {
					Total++;
				}
			}

			return Total;
		}
		public virtual bool CanConnect(RoomDef r, Dot p) {
			// The point must sit on an edge of this room and inside it. The intersection of two
			// rooms that do not touch produces points in the gap between them, and a loose
			// edge-equality test would happily turn one of those into a door.
			var vertical = ((int) p.X == Left || (int) p.X == Right) && p.Y > Top && p.Y < Bottom;
			var horizontal = ((int) p.Y == Top || (int) p.Y == Bottom) && p.X > Left && p.X < Right;

			return vertical != horizontal;
		}
		public bool ConnectTo(RoomDef Other) {
			if (Neighbours.Contains(Other)) {
				return true;
			}

			// Intersect normalises an inverted rect, so the width/height test below cannot tell a
			// shared wall from the gap between two rooms that never touch. Check the rectangles.
			if (Left > Other.Right || Other.Left > Right || Top > Other.Bottom || Other.Top > Bottom) {
				return false;
			}

			var I = Intersect(Other);
			var W = I.GetWidth();
			var H = I.GetHeight();

			if ((W == 0 && H >= 2) || (H == 0 && W >= 2)) {
				Neighbours.Add(Other);
				Other.Neighbours.Add(this);

				return true;
			}

			return false;
		}
		protected Dot GetDoorCenter() {
			var DoorCenter = new Dot(0, 0);

			foreach (var Door in Connected.Values) {
				DoorCenter.X += Door.X;
				DoorCenter.Y += Door.Y;
			}

			var N = Connected.Count;
			var C = new Dot(DoorCenter.X / N, DoorCenter.Y / N);

			if (Rnd.Float() < DoorCenter.X % 1) {
				C.X++;
			}

			if (Rnd.Float() < DoorCenter.Y % 1) {
				C.Y++;
			}

			C.X = (int) MathUtils.Clamp(Left + 1, Right - 1, C.X);
			C.Y = (int) MathUtils.Clamp(Top + 1, Bottom - 1, C.Y);

			return C;
		}
	}
}
