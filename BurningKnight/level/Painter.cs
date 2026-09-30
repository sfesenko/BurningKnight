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
		public static bool AllGold;

		public float Cobweb = 0.2f;
		public float Dirt = 0.4f;
		public float Grass = 0.4f;
		public float Water = 0.4f;
		public float Fireflies = 1f;
		public float FirefliesChance = 60f;
		public List<Action<Level, RoomDef>> RoomModifiers = new List<Action<Level, RoomDef>>();
		public List<Action<Level, RoomDef, int, int>> Modifiers = new List<Action<Level, RoomDef, int, int>>();
		public Tile DirtTile = Tile.Dirt;
		public Action<List<MobInfo>> ModifyMobs;

		public Painter() {
			AllGold = false;
			
			// All the rocks, that have not full neighbours will become metal blocks (33% chance)
			RoomModifiers.Add((l, r) => {
				if (Rnd.Chance(66)) {
					return;
				}
				
				for (var y = r.Top; y <= r.Bottom; y++) {
					for (var x = r.Left; x <= r.Right; x++) {
						var index = l.ToIndex(x, y);

						if (l.Get(index, true) == Tile.Rock) {
							var sum = 0;

							foreach (var dir in PathFinder.Neighbours8) {
								var n = dir + index;

								if (l.IsInside(n) && (TileFlags.Matches(l.Tiles[n], TileFlags.Solid) ||
								                      TileFlags.Matches(l.Liquid[n], TileFlags.HalfWall))) {
									sum++;
								}
							}

							if (sum > 0 && sum < 8) {
								l.Set(index, Tile.MetalBlock);

								return;
							}
						}
					}
				}
			});
			
			// Small chance to replace rock with a tinted rock or with a barrel
			Modifiers.Add((l, rm, x, y) => {
				var index = l.ToIndex(x, y);

				if (l.Get(index, true) == Tile.Rock) {
					var r = Rnd.Float();

					if (r <= 0.05f) {
						l.Set(index, Tile.TintedRock);
					} else if (Context.Run.Depth > 0 && r <= 0.1f && !(rm is TreasureRoom)) {
						l.Set(index, Tile.BarrelTmp);
					}
				}
			});
		}
		
		private void InspectRoom(RoomDef room) {
			foreach (var r in room.Connected.Keys) {
				if (r.Distance == -1) {
					r.Distance = room.Distance + 1;
					InspectRoom(r);
				}
			}
		}

		public bool Paint(Level Level, List<RoomDef> Rooms) {
			if (Rooms == null) {
				return false;
			}

			Level.Rains = Context.Run.Depth > 0 && Rnd.Chance(15);

			if (Level.Biome.Id == Biome.Ice) {
				Level.Snows = true;
				Level.Rains = false;
			} else if (Level.Biome.Id == Biome.Castle) {
				Level.Snows = Context.Run.Depth > 0 && Rnd.Chance(10);
			} else if (Level.Biome.Id == Biome.Desert) {
				Level.Rains = false;
			}

			// Level.Dark = Run.Depth > 1 && Rnd.Chance(5);

			if (Context.Run.Depth == 5 && LevelSave.GenerateMarket && Context.Run.Loop == 0) {
				Level.Dark = true;
			}
			
			RoomDef current = null;

			foreach (var r in Rooms) {
				if (r is ExitRoom) {
					current = r;
					break;
				}
			}

			if (current == null) {
				Log.Error("Failed to find the exit room");
			} else {
				current.Distance = 0;
				InspectRoom(current);
			}

			var LeftMost = int.MaxValue;
			var TopMost = int.MaxValue;

			foreach (var R in Rooms) {
				if (R.Left < LeftMost) {
					LeftMost = R.Left;
				}

				if (R.Top < TopMost) {
					TopMost = R.Top;
				}
			}

			LeftMost--;
			TopMost--;
			var Sz = Level.GetPadding();
			LeftMost -= Sz;
			TopMost -= Sz;
			var RightMost = 0;
			var BottomMost = 0;

			foreach (var R in Rooms) {
				R.Shift(-LeftMost, -TopMost);

				if (R.Right > RightMost) {
					RightMost = R.Right;
				}

				if (R.Bottom > BottomMost) {
					BottomMost = R.Bottom;
				}
			}

			RightMost++;
			BottomMost++;
			RightMost += Sz;
			BottomMost += Sz;
			
			Log.Info($"Setting level size to {(1 + RightMost)}:{(BottomMost + 1)}");
			
			Level.Width = RightMost + 1;
			Level.Height = BottomMost + 1;
			
			Level.Setup();

			var tile = Level.GetFilling();
			var liquid = tile.Matches(TileFlags.LiquidLayer);

			if (liquid) {
				var t = (byte) Tile.FloorA;
				
				for (int i = 0; i < Level.Size; i++) {
					Level.Tiles[i] = t;
					Level.Liquid[i] = (byte) tile;
				}
			} else {
				for (int i = 0; i < Level.Size; i++) {
					Level.Tiles[i] = (byte) tile;
				}	
			}

			if (Level.Biome is IceBiome || (Context.Run.Depth > 0 && tile == Tile.Chasm)) {
				var z = Level.Biome is IceBiome ? Tile.WallB : Tile.WallA;
				
				for (var x = 0; x < Level.Width; x++) {
					for (var y = 0; y < 5; y++) {
						if (y == 0 || Rnd.Chance((5 - y) * 20)) {
							Set(Level, x, y, z);
						}
					}
					
					for (var y = Level.Height - 6; y < Level.Height; y++) {
						if (y == Level.Height - 1 || Rnd.Chance((y - Level.Height + 5) * 20)) {
							Set(Level, x, y, z);
						}
					}
				}
				
				for (var y = 0; y < Level.Height; y++) {
					for (var x = 0; x < 5; x++) {
						if (x == 0 || Rnd.Chance((5 - x) * 20)) {
							Set(Level, x, y, z);
						}
					}
					
					for (var x = Level.Width - 6; x < Level.Width; x++) {
						if (x == Level.Width - 1 || Rnd.Chance((x - Level.Width + 5) * 20)) {
							Set(Level, x, y, z);
						}
					}
				}
			}

			var tr = Level.GetFilling();
			RoomDef exit = null;
			RoomDef entrance = null;

			for (var i = Rooms.Count - 1; i >= 0; i--) {
				var Room = Rooms[i];
				PlaceDoors(Room);

				if (Room is EntranceRoom) {
					entrance = Room;
				} else if (Room is ExitRoom) {
					exit = Room;
				}

				foreach (var d in Room.Connected.Values) {
					if (d.Type != DoorPlaceholder.Variant.Empty && d.Type != DoorPlaceholder.Variant.Secret &&
					    d.Type != DoorPlaceholder.Variant.Maze) {

						if (d.X == Room.Left || d.X == Room.Right) {
							Set(Level, d.X, d.Y - 1, tr);
							Set(Level, d.X, d.Y + 1, tr);
						} else {
							Set(Level, d.X - 1, d.Y, tr);
							Set(Level, d.X + 1, d.Y, tr);
						}
					}
				}
			}

			// Not checking lib cuz teleporters
			var check = Context.Run.Depth > 0 && !(LevelSave.BiomeGenerated is LibraryBiome);

			if (check && (exit == null || entrance == null)) {
				Log.Error("Exit or entrance not found, aborting");
				return false;
			}

			var fl = Level.GetFilling() == Tile.WallB ? Tile.WallB : Tile.WallA;

			for (var i = Rooms.Count - 1; i >= 0; i--) {
				var Room = Rooms[i];
			
				if (!(Room is ConnectionRoom)) {
					Rect(Level, Room, 0, LevelSave.BiomeGenerated is IceBiome ? Tile.WallB : fl);
				}

				Clip = Room.Shrink();

				Room.PaintFloor(Level);
				Room.Paint(Level);

				if (Room is SecretRoom) {
					for (var Y = Room.Top + 1; Y < Room.Bottom; Y++) {
						for (var X = Room.Left + 1; X < Room.Right; X++) {
							if (Rnd.Chance(Context.Run.Depth * 5)) {
								Level.MatrixLeak[Level.ToIndex(X, Y)] = true;
							}
						}
					}
				}

				if (!(Room is TreasureRoom)) {
					foreach (var d in Room.Connected.Values) {
						if (d.Type != DoorPlaceholder.Variant.Secret) {
							var a = d.X == Room.Left || d.X == Room.Right;
							var w = a ? 2 : 1;
							var h = a ? 1 : 2;
							var f = Tiles.RandomFloor();

							Call(Level, d.X - w, d.Y - h, w * 2 + 1, h * 2 + 1, (x, y) => {
								if (Level.Get(x, y).Matches(TileFlags.Danger)) {
									Level.Set(x, y, f);
								}
							});
						}
					}
				}

				Clip = null;
				
				Room.SetupDoors(Level);

				foreach (var m in RoomModifiers) {
					m(Level, Room);
				}

				for (var Y = Room.Top; Y <= Room.Bottom; Y++) {
					for (var X = Room.Left; X <= Room.Right; X++) {
						foreach (var m in Modifiers) {
							m(Level, Room, X, Y);
						}
					}
				}

				ReplaceTiles(Level, Room);
			}

			PathFinder.SetMapSize(Level.Width, Level.Height);

			if (Context.Run.Depth > -1) {
				if (Dirt > 0) {
					PaintDirt(Level, Rooms);
				}

				if (Grass > 0) {
					PaintGrass(Level, Rooms);
				}

				if (Cobweb > 0) {
					PaintCobweb(Level, Rooms);
				}

				if (Water > 0) {
					PaintWater(Level, Rooms);
				}
			}

			PaintDoors(Level, Rooms);
			Decorate(Level, Rooms);

			UpdateTransition(Level);

			if (check) {
				var c = exit.GetCenter();
				Level.CreatePassable(true);
				PathFinder.SetMapSize(Level.Width, Level.Height);
				var i1 = Level.ToIndex(c.X, c.Y);
				
				PathFinder.BuildDistanceMap(i1, Level.Passable);
				c = entrance.GetCenter();

				var i2 = Level.ToIndex(c.X, c.Y);
				if (PathFinder.Distance[i2] == Int32.MaxValue) {
					Log.Error("Generated unpassable level, aborting");
				//	return false;
				}
			}

			var rooms = new List<RoomDef>();

			foreach (var rr in Rooms) {
				if (rr is RegularRoom || rr is EntranceRoom) {
					rooms.Add(rr);
				}
			}

			var rrms = new List<RoomDef>();

			foreach (var rm in rooms) {
				if (rm is RegularRoom) {
					rrms.Add(rm);
				}
			}

			if (rrms.Count > 0) {
				foreach (var type in Level.ItemsToSpawn) {
					var item = Items.CreateAndAdd(type, Level.Area);

					if (item == null) {
						continue;
					}
					
					item.Center = (rrms[Rnd.Int(rrms.Count)].GetRandomFreeCell() * 16) + new Vector2(8, 8);
				}

				if (Context.Run.Depth == 1) {
					var crystal = new Crystal();
					crystal.Center = (rrms[Rnd.Int(rrms.Count)].GetRandomFreeCell() * 16) + new Vector2(8, 8) + Rnd.Vector(-4, 4);
					Level.Area.Add(crystal);
				}
			} else {
				Log.Error("Failed to place items");
			}

			Level.ItemsToSpawn = null;

			var rms = new List<Room>();
			
			foreach (var def in Rooms) {
				if (!def.ConvertToEntity()) {
					continue;
				}
				
				var room = new Room();

				room.Type = RoomDef.DecideType(def, def.GetType());
				room.MapX = def.Left;
				room.MapY = def.Top;
				room.MapW = def.GetWidth();
				room.MapH = def.GetHeight();
				room.Parent = def;
				
				Level.Area.Add(room);
				rms.Add(room);

				def.ModifyRoom(room);

				room.Generate();
			}
			
			PlaceMobs(Level, rms, ModifyMobs);
			return true;
		}

		public static void UpdateTransition(Level Level) {
			for (var y = 6; y < Level.Height - 5; y++) {
				for (var x = 6; x < Level.Width - 5; x++) {
					if (Level.Get(x, y) == Tile.WallA) {
						var a = (y == 0 || y == Level.Height - 1 || x == 0 || x == Level.Width - 1);

						if (!a) {
							a = true;

							foreach (var d in MathUtils.AllDirections) {
								if (!Level.Get(x + (int) d.X, y + (int) d.Y).Matches(Tile.WallA, Tile.Transition)) {
									a = false;

									break;
								}
							}
						}

						if (a) {
							Level.Set(x, y, Tile.Transition);
						}
					}
				}
			}
		}
	}
}