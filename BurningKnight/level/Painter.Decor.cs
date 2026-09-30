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
		public static void PlaceTrees(Level Level, RoomDef Room) {
			for (var Y = Room.Top - 1; Y < Room.Bottom - 1; Y++) {
				for (var X = Room.Left - 1; X < Room.Right - 1; X++) {
					if (Level.Get(X, Y).IsWall() && Level.Get(X, Y - 1).IsWall() &&
					    Level.Get(X - 1, Y).IsWall() && Level.Get(X + 1, Y).IsWall()
					    && !Room.HasDoorsNear(X, Y, 3)
					    && Rnd.Chance(10)) {

						X += 2;
						var plant = new Tree {
							High = true
						};
								
						Level.Area!.Add(plant);

						plant.BottomCenter = new Vector2(X * 16 + 8 + Rnd.Float(-4, 4), Y * 16 + 8 + Rnd.Float(-4, 4));
					} else if (Level.Get(X, Y).IsPassable() && Level.Get(X, Y - 1).IsPassable() &&
					           Level.Get(X - 1, Y).IsPassable() && Level.Get(X - 1, Y - 1).IsPassable() &&
					           Level.Get(X + 1, Y).IsPassable() && Level.Get(X + 1, Y - 1).IsPassable() &&
					           Level.Get(X - 1, Y - 2).IsPassable() && Level.Get(X + 1, Y - 2).IsPassable()
					           && !Room.HasDoorsNear(X, Y, 3)
					           && Rnd.Chance(6)) {

						X += 3;
						var plant = new Tree();
						Level.Area!.Add(plant);

						plant.BottomCenter = new Vector2(X * 16 + 8 + Rnd.Float(-4, 4), Y * 16 + 8 + Rnd.Float(-4, 4));
					}
				}
			}
		}
		public static void PlacePlants(Level Level, RoomDef Room) {
			for (var Y = Room.Top; Y <= Room.Bottom; Y++) {
				for (int X = Room.Left; X <= Room.Right; X++) {
					if ((Level.Get(X, Y, true).Matches(Tile.Grass, Tile.Dirt) && Rnd.Chance(20)) || (Level.Get(X, Y).Matches(TileFlags.Passable) && Rnd.Chance(5))) {
						var plant = new Plant();
						Level.Area!.Add(plant);

						plant.BottomCenter = new Vector2(X * 16 + 8 + Rnd.Float(-4, 4), Y * 16 + 8 + Rnd.Float(-4, 4));
					}
				}
			}
		}
		protected void Decorate(Level Level, List<RoomDef> Rooms) {
			foreach (var Room in Rooms) {
				// Tnt

				if (Level.Biome.HasTnt()) {
					if ((Room is RegularRoom) && Rnd.Chance(20)) {
						for (var i = 0; i < Rnd.Int(1, 4); i++) {
							var p = Room.GetRandomDoorFreeCell();

							if (p != null) {
								var barrel = new ExplodingBarrel();
								Level.Area!.Add(barrel);
								barrel.Center = p * 16 + new Vector2(8);
							}
						}
					}
				}

				// Plants
				if (Level.Biome.HasPlants()) {
					PlacePlants(Level, Room);
				}

				if (Events.Halloween) {
					for (var Y = Room.Top; Y <= Room.Bottom; Y++) {
						for (int X = Room.Left; X <= Room.Right; X++) {
							if ((Level.Get(X, Y, true).Matches(Tile.Grass, Tile.Dirt) && Rnd.Chance(20)) || (Level.Get(X, Y).Matches(TileFlags.Passable) && Rnd.Chance(0.5f))) {
								var plant = new Plant();
								Level.Area!.Add(plant);
								plant.Variant = 255;
								plant.BottomCenter = new Vector2(X * 16 + 8 + Rnd.Float(-4, 4), Y * 16 + 8 + Rnd.Float(-4, 4));
							}
						}
					}
				}

				if (!(Room is HiveRoom) && Level.Biome.HasTrees()) {
					PlaceTrees(Level, Room);
				}

				// Fireflies
				if (Level.Dark || Rnd.Chance(FirefliesChance)) {
					for (var I = 0; I < (!Level.Dark && Rnd.Chance() ? 1 : Rnd.Int(3, 6)) * Fireflies; I++) {
						Level.Area!.Add(new Firefly {
							X = (Room.Left + 2) * 16 + Rnd.Float((Room.GetWidth() - 4) * 16),
							Y = (Room.Top + 2) * 16 + Rnd.Float((Room.GetHeight() - 4) * 16)
						});
					}
				}

				// Cobweb
				if (!(Room is BossRoom) && Level.Biome.HasCobwebs()) {
					for (var Y = Room.Top; Y <= Room.Bottom; Y++) {
						for (int X = Room.Left; X <= Room.Right; X++) {
							if (Level.Get(X, Y).IsSimpleWall()) {
								if (Y > Room.Top && X > Room.Left && Level.Get(X - 1, Y - 1).IsSimpleWall() && !Level.Get(X, Y - 1).IsSimpleWall() && Rnd.Chance(20)) {
									Level.Area!.Add(new SlicedProp("cobweb_c", Layers.WallDecor) {
										X = X * 16,
										Y = Y * 16 - 24
									});
								} else if (Y > Room.Top && X < Room.Right && Level.Get(X + 1, Y - 1).IsSimpleWall() && !Level.Get(X, Y - 1).IsSimpleWall() && Rnd.Chance(20)) {
									Level.Area!.Add(new SlicedProp("cobweb_d", Layers.WallDecor) {
										X = X * 16,
										Y = Y * 16 - 24
									});
								} else if (Y < Room.Bottom - 1 && X > Room.Left && Level.Get(X - 1, Y + 1).IsSimpleWall() && !Level.Get(X, Y + 1).IsSimpleWall() && Rnd.Chance(20)) {
									Level.Area!.Add(new SlicedProp("cobweb_a", Layers.WallDecor) {
										X = X * 16,
										Y = Y * 16 + 8
									});
								} else if (Y < Room.Bottom - 1 && X < Room.Right && Level.Get(X + 1, Y + 1).IsSimpleWall() && !Level.Get(X, Y + 1).IsSimpleWall() && Rnd.Chance(20)) {
									Level.Area!.Add(new SlicedProp("cobweb_b", Layers.WallDecor) {
										X = X * 16,
										Y = Y * 16 + 8
									});
								}
							}
						}
					}
				}

				
				if (!(Room is SecretRoom || Room is TreasureRoom || Room is RegularRoom || Room is EntranceRoom || Room is ConnectionRoom) || Context.Run.Depth < 1) {
					continue;
				}

				// Paintings && Torches
				var ht = Level.Biome.HasTorches();
				var hp = Level.Biome.HasPaintings();
				
				if (ht || hp) {
					for (int X = Room.Left + 1; X < Room.Right; X++) {
						var s = Room is SecretRoom;
						var t = Level.Get(X, Room.Top);

						if (t != Tile.Crack && t.IsWall() && !Level.Get(X, Room.Top + 1).IsWall() && Rnd.Chance(s ? 50 : 30)) {
							if (!s && Rnd.Chance()) {
								if (ht) {
									var torch = new WallTorch();
									Level.Area!.Add(torch);
									torch.CenterX = X * 16 + 8 + Rnd.Float(-1, 1);
									torch.CenterY = Room.Top * 16 + 13;
								}
							} else if (hp) {
								var painting = PaintingRegistry.Generate(Level.Biome);
								Level.Area!.Add(painting);

								painting.CenterX = X * 16 + 8 + Rnd.Float(-1, 1);
								painting.Bottom = Room.Top * 16 + 17;
							}
						}
					}
				}

				if (!Level.Biome.HasBrekables() || Room is SecretRoom || Room is TreasureRoom || Room is ConnectionRoom || Room is EntranceRoom) {
					continue;
				}

				var types = new List<string>();
				var infos = Level.Biome is IceBiome ? BreakableProp.IceInfos : BreakableProp.Infos;

				for (var i = 0; i < Rnd.Int(2, 3); i++) {
					types.Add(infos[Rnd.Int(infos.Length)]);
				}
				
				for (int i = 0; i < Rnd.IntCentred(2, 7); i++) {
					var prop = new BreakableProp {
						Sprite = types[Rnd.Int(types.Count)]
					};
					
					var point = Room.GetRandomDoorFreeCell();

					if (point == null) {
						continue;
					}
					
					Level.Area!.Add(prop);
					prop.Center = new Vector2(point.X * 16 + 8 + Rnd.Float(-3, 3), point.Y * 16 + 8 + Rnd.Float(-3, 3));
				}
			}
		}
	}
}
