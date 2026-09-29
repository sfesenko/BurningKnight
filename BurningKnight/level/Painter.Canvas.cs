using System;
using System.Collections.Generic;
using System.Linq;
using BurningKnight.assets.items;
using BurningKnight.assets.prefabs;
using BurningKnight.entity;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.door;
using BurningKnight.entity.fx;
using BurningKnight.entity.item;
using BurningKnight.entity.room;
using BurningKnight.entity.room.controllable;
using BurningKnight.entity.room.controllable.spikes;
using BurningKnight.entity.room.input;
using BurningKnight.level.biome;
using BurningKnight.level.entities;
using BurningKnight.level.entities.decor;
using BurningKnight.level.entities.plant;
using BurningKnight.level.paintings;
using BurningKnight.level.rooms;
using BurningKnight.level.rooms.boss;
using BurningKnight.level.rooms.connection;
using BurningKnight.level.rooms.entrance;
using BurningKnight.level.rooms.regular;
using BurningKnight.level.rooms.secret;
using BurningKnight.level.rooms.treasure;
using BurningKnight.level.tile;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using BurningKnight.util.geometry;
using Lens.util;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.level {
	public partial class Painter {
		public static Rect Clip;
		public static void Set(Level Level, int cell, Tile Value) {
			if (Clip != null && !Clip.Contains(Level.FromIndexX(cell), Level.FromIndexY(cell))) {
				return;
			}

			Level.Set(cell, Value);
		}
		public static void Set(Level Level, int X, int Y, Tile Value, bool bold = false, bool walls = false) {
			if (bold) {
				SetBold(Level, X, Y, Value, walls);
				return;
			}
			
			Set(Level, Level.ToIndex(X, Y), Value);
		}
		public static void SetBold(Level Level, int X, int Y, Tile Value, bool walls = false) {
			for (var Yy = Y - 1; Yy < Y + 2; Yy++) {
				for (var Xx = X - 1; Xx < X + 2; Xx++) {
					if (!Level.IsInside(Xx, Yy)) {
						continue;
					}
					
					if (Xx != X || Yy != Y) {
						if (!walls && Level.Get(Xx, Yy).IsWall()) {
							continue;
						}
					}

					Set(Level, Xx, Yy, Value);
				}
			}
		}
		public static void Set(Level Level, Dot P, Tile Value) {
			Set(Level, (int) P.X, (int) P.Y, Value);
		}
		public static void Call(Level Level, int X, int Y, int W, int H, Action<int, int> callback) {
			for (var Yy = Y; Yy < Y + H; Yy++) {
				for (var Xx = X; Xx < X + W; Xx++) {
					callback(Xx, Yy);
				}
			}
		}
		public static void Call(Level level, Rect rect, int m, Action<int, int> callback) {
			rect = rect.Shrink(m);
			Call(level, rect.Left, rect.Top, rect.GetWidth(), rect.GetHeight(), callback);
		}
		public static void Fill(Level Level, int X, int Y, int W, int H, Tile Value) {
			for (var Yy = Y; Yy < Y + H; Yy++) {
				for (var Xx = X; Xx < X + W; Xx++) {
					Set(Level, Xx, Yy, Value);
				}
			}
		}
		public static void Rect(Level level, Rect rect, int m, Tile value, bool bold = false) {
			rect = rect.Shrink(m);
			Rect(level, rect.Left, rect.Top, rect.GetWidth(), rect.GetHeight(), value, bold);
		}
		public static void Rect(Level level, int X, int Y, int W, int H, Tile value, bool bold = false) {
			DrawLine(level, new Dot(X, Y), new Dot(X + W, Y), value, bold);
			DrawLine(level, new Dot(X, Y + H), new Dot(X + W, Y + H), value, bold);
			DrawLine(level, new Dot(X, Y), new Dot(X, Y + H), value, bold);
			DrawLine(level, new Dot(X + W, Y), new Dot(X + W, Y + H), value, bold);
		}
		public static void Triangle(Level Level, Dot From, Dot P1, Dot P2, Tile V) {
			if ((int) P1.X != (int) P2.X) {
				for (var X = P1.X; X < P2.X; X++) {
					DrawLine(Level, From, new Dot(X, P1.Y), V);
				}
			} else {
				for (var Y = P1.Y; Y < P2.Y; Y++) {
					DrawLine(Level, From, new Dot(P1.X, Y), V);
				}
			}
		}
		public static void Fill(Level Level, Rect Rect, Tile Value) {
			Fill(Level, Rect.Left, Rect.Top, Rect.GetWidth(), Rect.GetHeight(), Value);
		}
		public static void Fill(Level Level, Rect Rect, int M, Tile Value) {
			Fill(Level, Rect.Left + M, Rect.Top + M, Rect.GetWidth() - M * 2, Rect.GetHeight() - M * 2, Value);
		}
		public static void Fill(Level Level, Rect Rect, int L, int T, int R, int B, Tile Value) {
			Fill(Level, Rect.Left + L, Rect.Top + T, Rect.GetWidth() - (L + R), Rect.GetHeight() - (T + B), Value);
		}
		public static void DrawLine(Level Level, Dot From, Dot To, Tile Value, bool Bold = false) {
			float X = From.X;
			float Y = From.Y;
			float Dx = To.X - From.X;
			float Dy = To.Y - From.Y;
			var MovingbyX = Math.Abs(Dx) >= Math.Abs(Dy);

			if (MovingbyX) {
				Dy /= Math.Abs(Dx);
				Dx /= Math.Abs(Dx);
			} else {
				Dx /= Math.Abs(Dy);
				Dy /= Math.Abs(Dy);
			}

			if (Bold) {
				SetBold(Level, (int) Math.Round(X), (int) Math.Round(Y), Value);
			} else {
				Set(Level, (int) Math.Round(X), (int) Math.Round(Y), Value);
			}

			while (MovingbyX && (int) To.X != (int) X || !MovingbyX && (int) To.Y != (int) Y) {
				X += Dx;
				Y += Dy;

				if (Bold) {
					SetBold(Level, (int) Math.Round(X), (int) Math.Round(Y), Value);
				} else {
					Set(Level, (int) Math.Round(X), (int) Math.Round(Y), Value);
				}
			}
		}
		public static void FillEllipse(Level Level, Rect Rect, Tile Value) {
			FillEllipse(Level, Rect.Left, Rect.Top, Rect.GetWidth(), Rect.GetHeight(), Value);
		}
		public static void FillEllipse(Level Level, Rect Rect, int M, Tile Value) {
			Rect = Rect.Shrink(M);
			FillEllipse(Level, Rect.Left, Rect.Top, Rect.GetWidth(), Rect.GetHeight(), Value);
		}
		public static void FillEllipse(Level Level, int X, int Y, int W, int H, Tile Value) {
			double RadH = H / 2f;
			double RadW = W / 2f;

			for (var I = 0; I < H; I++) {
				var RowY = -RadH + 0.5 + I;
				var RowW = 2.0 * Math.Sqrt(RadW * RadW * (1.0 - RowY * RowY / (RadH * RadH)));

				if (W % 2 == 0) {
					RowW = Math.Round(RowW / 2.0) * 2.0;
				} else {
					RowW = Math.Floor(RowW / 2.0) * 2.0;
					RowW++;
				}

				var Cell = X + (W - (int) RowW) / 2 + (Y + I) * Level.Width;

				for (var J = Cell; J < Cell + RowW; J++) {
					Level.Set(J, Value);
				}
			}
		}
		public static void Ellipse(Level Level, Rect Rect, int m, Tile Value, bool bold = false) {
			Rect = Rect.Shrink(m);
			Ellipse(Level, Rect.Left, Rect.Top, Rect.GetWidth(), Rect.GetHeight(), Value, bold);
		}
		public static void Ellipse(Level Level, Rect Rect, Tile Value, bool bold = false) {
			Ellipse(Level, Rect.Left, Rect.Top, Rect.GetWidth(), Rect.GetHeight(), Value, bold);
		}
		// To be tested
		public static void Ellipse(Level Level, int X, int Y, int W, int H, Tile Value, bool bold) {
			double RadH = H / 2f;
			double RadW = W / 2f;

			for (var I = 0; I < H; I++) {
				var RowY = -RadH + 0.5 + I;
				var RowW = 2.0 * Math.Sqrt(RadW * RadW * (1.0 - RowY * RowY / (RadH * RadH)));

				if (W % 2 == 0) {
					RowW = Math.Round(RowW / 2.0) * 2.0;
				} else {
					RowW = Math.Floor(RowW / 2.0) * 2.0;
					RowW++;
				}

				var Cell = X + (W - (int) RowW) / 2 + (Y + I) * Level.Width;
				var CellB = (int) (Cell + RowW - 1);

				if (I == 0 || I == H - 1) {
					for (var J = Cell - 1; J <= Cell + RowW; J++) {
						if (bold) {
							SetBold(Level, Level.FromIndexX(J), Level.FromIndexY(J), Value);
						} else {
							Level.Set(J, Value);
						}
					}
				} else {
					if (bold) {
						SetBold(Level, Level.FromIndexX(Cell), Level.FromIndexY(Cell), Value);
						SetBold(Level, Level.FromIndexX(CellB), Level.FromIndexY(CellB), Value);	
					} else {
						Level.Set(Cell, Value);
						Level.Set(CellB, Value);	
					}
				}
			}
		}
		public static void Prefab(Level level, string id, int x, int y) {
			var prefab = Prefabs.Get(id);

			if (prefab == null) {
				Log.Error($"Unknown prefab {id}");
				return;
			}
			
			prefab.Place(level, x, y);
		}
	}
}
