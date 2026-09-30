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
		public Dot GetRandomFreeCell() {
			var passable = new List<Dot>();

			for (var x = Left + 1; x < Right; x++) {
				for (var y = Top + 1; y < Bottom; y++) {
					if (Context.Level.IsPassable(x, y)) {
						passable.Add(new Dot(x, y));
					}
				}
			}

			if (passable.Count == 0) {
				Log.Error($"Failed to find a free cell ({GetType().Name})");
				return null;
			}

			return passable[Rnd.Int(passable.Count)];
		}
		public Dot GetRandomDoorFreeCell() {
			var passable = new List<Dot>();

			for (var x = Left + 1; x < Right; x++) {
				for (var y = Top + 1; y < Bottom; y++) {
					if (Context.Level.IsPassable(x, y)) {
						var found = false;
						
						foreach (var Door in Connected.Values) {
							var Dx = (int) (Door.X - x);
							var Dy = (int) (Door.Y - y);
							var D = (float) Math.Sqrt(Dx * Dx + Dy * Dy);

							if (D < 4) {
								found = true;
								break;
							}
						}

						if (!found) {
							passable.Add(new Dot(x, y));
						}
					}
				}
			}

			if (passable.Count == 0) {
				Log.Error($"Failed to find a free cell ({GetType().Name})");
				return null;
			}

			return passable[Rnd.Int(passable.Count)];
		}
		public bool SetSize() {
			return SetSize(GetMinWidth(), GetMaxWidth(), GetMinHeight(), GetMaxHeight());
		}
		public bool SetSizeWithLimit(int W, int H) {
			if (W < GetMinWidth() || H < GetMinHeight()) {
				return false;
			}

			SetSize();

			if (GetWidth() > W || GetHeight() > H) {
				var Ww = ValidateWidth(Math.Min(GetWidth(), W) - 1);
				var Hh = ValidateHeight(Math.Min(GetHeight(), H) - 1);

				if (Ww >= W || Hh >= H) {
					return false;
				}

				Resize(Ww, Hh);
			}

			return true;
		}
		public List<Dot> GetWaterPlaceablePoints() {
			var Points = new List<Dot>();

			for (var I = Left + 1; I <= Right - 1; I++) {
				for (var J = Top + 1; J <= Bottom - 1; J++) {
					var P = new Dot(I, J);

					if (CanPlaceWater(P)) {
						Points.Add(P);
					}
				}
			}

			return Points;
		}
		public List<Dot> GetGrassPlaceablePoints() {
			var Points = new List<Dot>();

			for (var I = Left + 1; I <= Right - 1; I++) {
				for (var J = Top + 1; J <= Bottom - 1; J++) {
					var P = new Dot(I, J);

					if (CanPlaceGrass(P)) {
						Points.Add(P);
					}
				}
			}

			return Points;
		}
	}
}
