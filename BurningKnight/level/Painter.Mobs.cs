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
		public static void PlaceMobs(Level level, Room room, Action<List<MobInfo>> modifier = null) {
			var parent = room.Parent;
			var w = parent.GetWidth() - 2;
			var h = parent.GetHeight() - 2;
			var mobs = new List<MobInfo>(MobRegistry.Current);
			
			parent.ModifyMobList(mobs);

			if (mobs.Count == 0) {
				return;
			}
			
			var chances = new List<float>();

			for (var i = 0; i < mobs.Count; i++) {
				chances.Add(parent.WeightMob(mobs[i], mobs[i].GetChanceFor(level.Biome.Id)));
			}

			var types = new List<MobInfo>();
			var spawnChances = new List<float>();
			var curseOfBlood = Scourge.IsEnabled(Scourge.OfBlood);

			if (level.Biome.SpawnAllMobs()) {
				types.AddRange(mobs);
				spawnChances.AddRange(chances);
			} else {
				for (int i = 0; i < Rnd.Int(2, 6) * (curseOfBlood ? 2 : 1); i++) {
					var ind = Rnd.Chances(chances);
					var type = mobs[ind];

					mobs.RemoveAt(ind);
					chances.RemoveAt(ind);
					types.Add(type);
					spawnChances.Add(type.Chance);

					if (mobs.Count == 0) {
						break;
					}
				}
			}

			modifier?.Invoke(types);

			// A hack to get around RaveCaveVariant having just crab in there
			while (spawnChances.Count > types.Count) {
				spawnChances.RemoveAt(spawnChances.Count - 1);
			}

			if (types.Count == 0) {
				Log.Warning($"No mobs detected to spawn in {level.Biome.Id} biome");
				return;
			}
			
			var points = new List<Dot>();
			var wallFreePoints = new List<Dot>();
			var wallPoints = new List<Dot>();
			var size = w * h;
			var patch = new bool[size];

			Func<int, int, int> toIndex = (x, y) => (x - parent.Left - 1) + (y - parent.Top - 1) * w;

			for (var y = parent.Top + 1; y < parent.Bottom; y++) {
				for (var x = parent.Left + 1; x < parent.Right; x++) {
					patch[toIndex(x, y)] = !Context.Level.IsPassable(x, y, true);
				}
			}

			var hasDoors = parent.Connected.Count > 0;
			Dot start = null;
			
			if (hasDoors) {
				PathFinder.SetMapSize(w, h);

				var door = parent.Connected.Values.First();
				start = new Dot(door.X, door.Y);

				if ((int) start.X == parent.Left) {
					start.X++;
				} else if ((int) start.Y == parent.Top) {
					start.Y++;
				} else if ((int) start.X == parent.Right) {
					start.X--;
				} else if ((int) start.Y == parent.Bottom) {
					start.Y--;
				}

				PathFinder.BuildDistanceMap(toIndex(start.X, start.Y), BArray.Not(patch, null));
			}

			for (var y = parent.Top + 1; y < parent.Bottom; y++) {
				for (var x = parent.Left + 1; x < parent.Right; x++) {
					if (hasDoors) {
						var i = toIndex(x, y);

						if (patch[i] || PathFinder.Distance[i] == Int32.MaxValue) {
							continue;
						}

						var found = false;

						foreach (var dr in parent.Connected.Values) {
							var dx = (int) (dr.X - x);
							var dy = (int) (dr.Y - y);
							var d = (float) Math.Sqrt(dx * dx + dy * dy);

							if (d < 4) {
								found = true;

								break;
							}
						}

						if (found) {
							continue;
						}
					}

					if (room.Type == RoomType.Boss) {
						var c = parent.GetCenter();
						
						var dx = (int) (c.X - x);
						var dy = (int) (c.Y - y);
						var d = (float) Math.Sqrt(dx * dx + dy * dy);

						if (d < 3) {
							continue;
						}
					}

					var dt = new Dot(x, y);

					if ((Context.Level.IsPassable(x - 1, y) || Context.Level.IsPassable(x + 1, y)) && (Context.Level.IsPassable(x, y + 1) || Context.Level.IsPassable(x, y - 1))) {
						points.Add(dt);
						
						if (Context.Level.IsPassable(x - 1, y) && Context.Level.IsPassable(x + 1, y) && Context.Level.IsPassable(x, y + 1) && Context.Level.IsPassable(x, y - 1)) {
							wallFreePoints.Add(dt);
						}
					}

					if (Context.Level.Get(x - 1, y).IsWall() || Context.Level.Get(x + 1, y).IsWall() || Context.Level.Get(x, y - 1).IsWall() || Context.Level.Get(x , y + 1).IsWall()) {
						wallPoints.Add(dt);
					}
				}
			}

			if (points.Count + wallPoints.Count == 0) {
				Log.Error("Did not find any placeable spots for mobs");
				return;
			}
			
			PathFinder.SetMapSize(level.Width, level.Height);

			var count = room.Parent.GetPassablePoints(level).Count;
			var weight = (count / 19f + Rnd.Float(0f, 1f)) * room.Parent.GetWeightModifier() * (curseOfBlood ? 2 : 1);

			if (Context.Run.Loop > 0) {
				weight *= Context.Run.Loop * 1.5f + 1f;
			}
			
			while (weight > 0 && (points.Count > 0 || wallPoints.Count > 0)) {
				var id = Rnd.Chances(spawnChances);

				if (id == -1) {
					Log.Error("Failed to generate mobs :O");
					break;
				}
				
				var type = types[id];
				Dot point = null;

				if (type.NearWall) {
					if (wallPoints.Count == 0) {
						continue;
					}

					var index = Rnd.Int(wallPoints.Count);
					point = wallPoints[index];
					wallPoints.RemoveAt(index);
				} else if (type.AwayFromWall) {
					if (wallFreePoints.Count == 0) {
						continue;
					}

					var index = Rnd.Int(wallFreePoints.Count);
					point = wallFreePoints[index];
					wallFreePoints.RemoveAt(index);
				} else {
					if (points.Count == 0) {
						continue;
					}

					var index = Rnd.Int(points.Count);
					point = points[index];
					points.RemoveAt(index);
				}

				if (point == null) {
					continue;
				}

				var mob = (Mob) Activator.CreateInstance(type.Type);
				
				weight -= type.Weight;
				level.Area.Add(mob);
				
				if (type.NearWall) {
					mob.Position = new Vector2(point.X * 16, point.Y * 16 - 8);
				} else {
					mob.BottomCenter = new Vector2(point.X * 16 + 8 + Rnd.Float(-2, 2), point.Y * 16 + 8 + Rnd.Float(-2, 2));
				}
				
				mob.GeneratePrefix();

				if (type.Single) {
					types.RemoveAt(id);
					spawnChances.RemoveAt(id);

					if (types.Count == 0) {
						return;
					}
				}
			}
		}
		private void PlaceMobs(Level level, List<Room> rooms, Action<List<MobInfo>> modifier = null) {
			MobRegistry.SetupForBiome(level.Biome.Id);
			// level.CreatePassable(true);
			
			foreach (var room in rooms) {
				if (room.Parent.ShouldSpawnMobs()) {
					PlaceMobs(level, room, modifier);
				}
			}	
		}
	}
}
