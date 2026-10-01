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
		private void PlaceDoors(RoomDef R) {
			var connected = new Dictionary<RoomDef, DoorPlaceholder>();

			foreach (var pair in R.Connected) {
				connected[pair.Key] = pair.Value!;
			}
			
			foreach (var N in connected.Keys) {
				var Door = connected[N];

				if (Door == null) {
					var I = R.Intersect(N);
					var DoorSpots = new List<Dot>();

					foreach (var P in I.GetPoints()) {
						if (R.CanConnect(N, P) && N.CanConnect(R, P)) {
							DoorSpots.Add(P);
						}
					}

					if (DoorSpots.Count > 0) {
						Door = new DoorPlaceholder(DoorSpots[Rnd.Int(DoorSpots.Count)]);
						R.Connected[N] = Door;
						N.Connected[R] = Door;
					} else {
						R.Connected.Remove(N);
						N.Connected.Remove(R);

						throw new LevelGenerationException($"Failed to connect rooms {R.GetType().Name} and {N.GetType().Name}");
					}
				}
			}
		}
		public static void PaintDoor(Level Level, RoomDef R) {
			foreach (var N in R.Connected.Keys) {
				var D = R.Connected[N];
				PlaceDoor(Level, R, D!, N);
			}
		}
		public static void PlaceDoor(Level Level, RoomDef R, DoorPlaceholder D, RoomDef from) {
			var T = Level.Get(D.X, D.Y);
			var type = D.Type;

			var gt = type != DoorPlaceholder.Variant.Empty && type != DoorPlaceholder.Variant.Maze && type != DoorPlaceholder.Variant.Secret;
			var vertical = Level.Get(D.X, D.Y + 1).IsWall() && Level.Get(D.X, D.Y - 1).IsWall();
			
			if (gt && !T.Matches(Tile.FloorA, Tile.FloorB, Tile.FloorC, Tile.FloorD, Tile.Crack)) {
				Door? door = null;

				switch (type) {
					case DoorPlaceholder.Variant.Locked: 
						door = new SpecialDoor();
						break;
					
					case DoorPlaceholder.Variant.Red: 
						door = new RedDoor();
						break;
					
					case DoorPlaceholder.Variant.Boss: 
						door = new BossDoor();
						break;
					
					case DoorPlaceholder.Variant.Treasure:
						door = new TreasureDoor();
						break;
					
					case DoorPlaceholder.Variant.Scourged:
						door = new ScourgedDoor();
						break;
					
					case DoorPlaceholder.Variant.Payed:
						door = new PayedDoor();
						break;
					
					case DoorPlaceholder.Variant.Head:
						// door = new HeadDoor();
						return;
					
					case DoorPlaceholder.Variant.Spiked:
						door = new SpikedDoor();
						break;
					
					case DoorPlaceholder.Variant.Challenge:
						door = new ChallengeDoor();
						break;
					
					case DoorPlaceholder.Variant.Shop:
						door = new ShopDoor();
						break;
				
					default: 
						door = new LockableDoor();
						break;
				}

				door.Vertical = vertical;
				Level.Area!.Add(door);

				var offset = door.GetOffset();

				door.CenterX = D.X * 16 + 8 + offset.X;
				door.Bottom = D.Y * 16 + 17.01f + offset.Y - (door is CustomDoor ? (door.Vertical ? 0 : 8) : 0); // .1f so that it's depth sorted to the front of the wall

				if (!(door is HeadDoor)) {
					if (door.Vertical) {
						if (type != DoorPlaceholder.Variant.Hidden) {
							if (!Level.Get(D.X + 1, D.Y).Matches(TileFlags.Passable)) {
								Level.Set(D.X + 1, D.Y, Tiles.RandomFloor());
							}

							if (!Level.Get(D.X - 1, D.Y).Matches(TileFlags.Passable)) {
								Level.Set(D.X - 1, D.Y, Tiles.RandomFloor());
							}
						}
					} else {
						if (type != DoorPlaceholder.Variant.Hidden) {
							if (!Level.Get(D.X, D.Y + 1).Matches(TileFlags.Passable)) {
								Level.Set(D.X, D.Y + 1, Tiles.RandomFloor());
							}

							if (!Level.Get(D.X, D.Y - 1).Matches(TileFlags.Passable)) {
								Level.Set(D.X, D.Y - 1, Tiles.RandomFloor());
							}
						}
					}

					Level.Set(D.X, D.Y, Tiles.RandomFloor());
				}
			} else if (type == DoorPlaceholder.Variant.Hidden) {
				Level.Set(D.X, D.Y, Level.Biome is IceBiome ? Tile.WallB : Tile.WallA);
			} else if (type == DoorPlaceholder.Variant.Secret) {
				Level.Set(D.X, D.Y, Tile.Crack);
				
				if (vertical) {
					if (Level.Get(D.X + 1, D.Y).Matches(Tile.WallA, Tile.WallB, Tile.Planks, Tile.Rock, Tile.TintedRock, Tile.MetalBlock)) {
						Level.Set(D.X + 1, D.Y, Tiles.RandomFloor());
					}

					if (!Level.Get(D.X - 1, D.Y).Matches(Tile.WallA, Tile.WallB, Tile.Planks, Tile.Rock, Tile.TintedRock, Tile.MetalBlock)) {
						Level.Set(D.X - 1, D.Y, Tiles.RandomFloor());
					}
				} else {
					if (!Level.Get(D.X, D.Y + 1).Matches(Tile.WallA, Tile.WallB, Tile.Planks, Tile.Rock, Tile.TintedRock, Tile.MetalBlock)) {
						Level.Set(D.X, D.Y + 1, Tiles.RandomFloor());
					}

					if (!Level.Get(D.X, D.Y - 1).Matches(Tile.WallA, Tile.WallB, Tile.Planks, Tile.Rock, Tile.TintedRock, Tile.MetalBlock)) {
						Level.Set(D.X, D.Y - 1, Tiles.RandomFloor());
					}
				}
			} else if (type == DoorPlaceholder.Variant.Empty) { 
				Level.Set(D.X, D.Y, Tiles.RandomFloor());
			}
		}
		private void PaintDoors(Level Level, List<RoomDef> Rooms) {
			foreach (var R in Rooms) {
				PaintDoor(Level, R);
			}
		}
	}
}
