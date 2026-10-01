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
	public partial class Room : SaveableEntity, PlaceableEntity {
		public int MapX;
		public int MapY;
		public int MapW = 4;
		public int MapH = 4;
		public TagLists Tagged = new TagLists();
		public RoomType Type;
		public bool Explored;
		public bool Cleared;
		public string Id = null!;
		public RoomDef Parent = null!;
		public Rect Rect = null!;
		
		public List<RoomControllable> Controllable = new List<RoomControllable>();
		public List<RoomInput> Inputs = new List<RoomInput>();
		public List<Piston> Pistons = new List<Piston>();
		public List<RoomController> Controllers = new List<RoomController>();
		public List<Door> Doors = new List<Door>();

		private bool checkCleared;
		private Entity cleared = null!;
		private float t;

		public void CheckCleared(Entity entity) {
			if (!Cleared) {
				Timer.Add(() => {
					checkCleared = true;
					cleared = entity;
				}, 0.1f);
			}
		}
		
		public override void AddComponents() {
			base.AddComponents();
			
			AddTag(Tags.Room);
		}

		public ItemPool GetPool() {
			switch (Type) {
				case RoomType.Shop: return ItemPool.Shop;
				case RoomType.Secret: return ItemPool.Secret;
				case RoomType.Boss: return ItemPool.Boss;
				case RoomType.Treasure: return ItemPool.Treasure;
			}

			return null;
		}

		public override void PostInit() {
			base.PostInit();

			UpdateSize();

			AlwaysActive = true;
			
			if (Type == RoomType.Shop || Type == RoomType.Treasure || Type == RoomType.Boss) {
				AddComponent(new LightComponent(this, 128f, new Color(1f, 0.9f, 0.5f, 0.8f)));
			}
		}
		
		public void UpdateSize() {
			X = MapX * 16 + 4;
			Y = MapY * 16 - 4;
			Width = MapW * 16 - 8;
			Height = MapH * 16 - 8;

			Rect = new Rect().Setup(MapX, MapY, MapW, MapH);
		}

		public override void Update(float dt) {
			base.Update(dt);

			t += dt;
			
			if (!settedUp && t >= 0.1f) {
				settedUp = true;
				Setup();

				if (!Engine.EditingLevel && Type == RoomType.Hidden) {
					Hide(true);
				}
			}

			foreach (var c in Controllers) {
				c.Update(dt);
			}

			if (checkCleared) {
				var found = false;

				foreach (var m in Tagged[Tags.MustBeKilled]) {
					if (m.GetComponent<HealthComponent>()!.Health > 0) {
						found = true;
						break;
					}
				}

				if (!found) {
					if (!Cleared) {
						SpawnReward();

						Cleared = true;
					
						cleared.HandleEvent(new RoomClearedEvent {
							Room = this
						});
					}
				}

				checkCleared = false;
			}
		}

		private bool settedUp;
		
		private void Setup() {
			var level = Context.Level;
			Explored = level!.Explored[level.ToIndex(MapX + 1, MapY + 1)];
			
			ApplyToEachTile((x, y) => {
				var tile = level.Get(x, y);

				if (tile.Matches(Tile.Piston, Tile.PistonDown)) {
					Pistons.Add(new Piston(x, y));
				}
			});
			
			foreach (var c in Controllers) {
				c.Init();
			}
		}

		public override void Destroy() {
			base.Destroy();
			
			foreach (var c in Controllers) {
				c.Destroy();
			}

			Pistons.Clear();
			Controllable.Clear();
			Inputs.Clear();
		}

		public void Discover() {
			Explored = true;
			
			ApplyToEachTile((x, y) => {
				Context.Level!.Explored[Context.Level!.ToIndex(x, y)] = true;
			});
		}

		public void Hide(bool fast = false) {
			Explored = false;
			
			ApplyToEachTile((x, y) => {
				var i = Context.Level!.ToIndex(x, y);

				if (!Context.Level!.Get(i).IsWall() || !Context.Level!.Get(i + Context.Level!.Width).IsWall()) {
					Context.Level!.Explored[i] = false;

					if (fast) {
						Context.Level!.Light[i] = 0;
					} else {
						Tween.To(0, 1f, xx => Context.Level!.Light[i] = xx, 0.5f);
					}
				}
			}, Type == RoomType.DarkMarket || Type == RoomType.Hidden ? 1 : 0);
		}
		
		public void Generate() {
			foreach (var c in Controllers) {
				c.Generate();
			}
		}

		public override void Load(FileReader stream) {
			base.Load(stream);

			MapX = stream.ReadInt16();
			MapY = stream.ReadInt16();
			MapW = stream.ReadInt16();
			MapH = stream.ReadInt16();
			
			Type = RoomRegistry.FromIndex(stream.ReadByte());
			
			var count = stream.ReadByte();

			for (var i = 0; i < count; i++) {
				var c = RoomControllerRegistery.Get(stream.ReadString()!);

				if (c != null) {
					Controllers.Add(c);
					c.Room = this;
					c.Load(stream);
				}
			}

			Id = stream.ReadString()!;
		}
		
		public override void Save(FileWriter stream) {
			base.Save(stream);
			
			stream.WriteInt16((short) MapX);
			stream.WriteInt16((short) MapY);
			stream.WriteInt16((short) MapW);
			stream.WriteInt16((short) MapH);

			stream.WriteByte((byte) RoomRegistry.FromType(Type));
			stream.WriteByte((byte) Controllers.Count);

			foreach (var c in Controllers) {
				stream.WriteString(c.Id);
				c.Save(stream);
			}

			stream.WriteString(Id);
		}
		
		protected int GetRenderLeft(Camera camera, Level level) {
			return (int) MathUtils.Clamp(0, level.Width - 1, Math.Max((int) Math.Floor(camera.X / 16 - 1f), MapX));
		}

		protected int GetRenderTop(Camera camera, Level level) {
			return (int) MathUtils.Clamp(0, level.Height - 1, Math.Max((int) Math.Floor(camera.Y / 16 - 1f), MapY));
		}

		protected int GetRenderRight(Camera camera, Level level) {
			return (int) MathUtils.Clamp(0, level.Width - 1, Math.Min((int) Math.Ceiling(camera.Right / 16 + 1f), MapX + MapW));
		}

		protected int GetRenderBottom(Camera camera, Level level) {
			return (int) MathUtils.Clamp(0, level.Height - 1, Math.Min((int) Math.Ceiling(camera.Bottom / 16 + 1f), MapY + MapH));
		}

		public override void RenderDebug() {
			Graphics.Batch.DrawRectangle(new RectangleF(X, Y, Width, Height), Color.Red);
		}

		public void AddController(string id) {
			var c = RoomControllerRegistery.Get(id);

			if (c != null) {
				Controllers.Add(c);
				c.Room = this;
				c.Init();
			}
		}

		public void HandleInputChange(RoomInput.ChangedEvent e) {
			foreach (var c in Controllers) {
				c.HandleInputChange(e);
			}
		}

		public Entity FindClosest(Vector2 to, int tag, Func<Entity, bool>? filter = null) {
			var min = float.MaxValue;
			Entity? en = null;
			
			foreach (var e in Tagged[tag]) {
				if (filter?.Invoke(e) ?? true) {
					var d = e.DistanceTo(to);

					if (d < min) {
						min = d;
						en = e;
					}
				}
			}

			return en;
		}
	}
}