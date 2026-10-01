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
		public static void ReplaceTiles(Level Level, RoomDef Room) {
			for (var Y = Room.Top; Y <= Room.Bottom; Y++) {
				for (var X = Room.Left; X <= Room.Right; X++) {
					var I = Level.ToIndex(X, Y);

					var rs = !Level!.Biome!.HasSpikes();

					if (rs) {
						var tl = (Tile) Level.Tiles[I];

						if (tl.Matches(Tile.SensingSpikeTmp, Tile.SpikeOnTmp, Tile.SpikeOffTmp, Tile.FireTrapTmp)) {
							Level.Set(I, Tile.FloorA);
						}
					} else {
						if (Level.Tiles[I] == (byte) Tile.SensingSpikeTmp) {
							Level.Tiles[I] = (byte) Tile.FloorA;
							Level.Liquid[I] = 0;

							var spikes = new SensingSpikes();

							spikes.X = X * 16;
							spikes.Y = Y * 16;

							Level.Area!.Add(spikes);
						} else if (Level.Tiles[I] == (byte) Tile.SpikeOffTmp) {
							Level.Tiles[I] = (byte) Tile.FloorA;
							Level.Liquid[I] = 0;

							var spikes = new Spikes();

							spikes.X = X * 16;
							spikes.Y = Y * 16;

							Level.Area!.Add(spikes);
						} else if (Level.Tiles[I] == (byte) Tile.FireTrapTmp) {
							Level.Tiles[I] = (byte) Tile.FloorA;
							Level.Liquid[I] = 0;

							var trap = new FireTrap();

							trap.X = X * 16;
							trap.Y = Y * 16;

							Level.Area!.Add(trap);
						} else if (Level.Tiles[I] == (byte) Tile.SpikeOnTmp) {
							Level.Tiles[I] = (byte) Tile.FloorA;
							Level.Liquid[I] = 0;

							var spikes = new AlwaysOnSpikes();

							spikes.X = X * 16;
							spikes.Y = Y * 16;

							Level.Area!.Add(spikes);
						}
					}

				if (Level.Tiles[I] == (byte) Tile.Plate) {
						Level.Tiles[I] = (byte) Tile.FloorA;
						Level.Liquid[I] = 0;
						
						var plate = new PreasurePlate();

						plate.X = X * 16;
						plate.Y = Y * 16;

						Level.Area!.Add(plate);
					} else if (Level.Tiles[I] == (byte) Tile.BarrelTmp) {
						Level.Tiles[I] = (byte) Tile.FloorA;
						Level.Liquid[I] = 0;
						
						var barrel = new ExplodingBarrel();
						Level.Area!.Add(barrel);

						barrel.CenterX = X * 16 + 8;
						barrel.Bottom = Y * 16 + 16;
					}
				}
			}
		}
		private void PaintWater(Level Level, List<RoomDef> Rooms) {
			var Lake = Patch.Noise(Water);
			var Ice = Level.Biome is IceBiome;

			foreach (var R in Rooms) {
				if (R is IceConnectionRoom) {
					continue;
				}
				
				var placed = false;
				
				foreach (var P in R.GetWaterPlaceablePoints()) {
					var I = Level.ToIndex((int) P.X, (int) P.Y);
					var T = (Tile) Level.Tiles[I];

					if (Lake[I] && T.Matches(Tile.FloorA, Tile.FloorB, Tile.FloorC, Tile.FloorD) && Level.Liquid[I] == 0) {
						Level.Set(I, Ice ? Tile.Ice : Tile.Water);
						placed = true;
					}
				}

				if (!placed) {
					var v = R.GetRandomFreeCell();

					if (v != null) {
						SetBold(Level, v.X, v.Y, Ice ? Tile.Ice : Tile.Water);
					}
				}
			}
		}
		private void PaintCobweb(Level Level, List<RoomDef> Rooms) {
			var Lake = Patch.Noise(Cobweb);

			foreach (var R in Rooms) {
				foreach (var P in R.GetWaterPlaceablePoints()) {
					var I = Level.ToIndex((int) P.X, (int) P.Y);
					var T = (Tile) Level.Tiles[I];
					
					if (Lake[I] && T.Matches(Tile.FloorA, Tile.FloorB, Tile.FloorC) && Level.Liquid[I] == 0) {
						Level.Set(I, Tile.Cobweb);
					}
				}
			}
		}
		private void PaintDirt(Level Level, List<RoomDef> Rooms) {
			var Grass = Patch.Noise(Dirt);
			var tile = Level.Biome is DesertBiome ? Tile.Sand : DirtTile;

			foreach (var R in Rooms) {
				foreach (var P in R.GetGrassPlaceablePoints()) {
					if (!Level.IsInside(P.X, P.Y)) {
						continue;
					}
					
					var I = Level.ToIndex((int) P.X, (int) P.Y);

					
					var T = (Tile) Level.Tiles[I];
					
					if (Grass[I] && T.Matches(Tile.FloorA, Tile.FloorB, Tile.FloorC) && Level.Liquid[I] == 0) {
						Level.Set(I, tile);
					}
				}
			}
		}
		private void PaintGrass(Level Level, List<RoomDef> Rooms) {
			var Grass = Patch.Noise(this.Grass);
			var Cells = new List<int>();

			foreach (var R in Rooms) {
				foreach (var P in R.GetGrassPlaceablePoints()) {
					var I = Level.ToIndex((int) P.X, (int) P.Y);
					var T = (Tile) Level.Tiles[I];
					
					if (Grass[I] && T.Matches(Tile.FloorA, Tile.FloorB, Tile.FloorC) && Level.Liquid[I] == 0) {
						Cells.Add(I);
					}
				}
			}

			foreach (var I in Cells) {
				var Count = 1;

				foreach (var N in PathFinder.Neighbours8) {
					var K = I + N;

					if (Level.IsInside(K) && Grass[K]) {
						Count++;
					}
				}

				Level.Set(I, Tile.Grass);
			}
		}
	}
}
