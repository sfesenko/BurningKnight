using BurningKnight.debug;
using System;
using System.Collections.Generic;
using BurningKnight.assets;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.fx;
using BurningKnight.entity.room;
using BurningKnight.level.biome;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.level.variant;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.graphics;
using Lens.graphics.gamerenderer;
using Lens.util;
using Lens.util.camera;
using Lens.util.file;
using Lens.util.math;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace BurningKnight.level {
	public partial class Level {
		private bool loadMarked;
		private bool first;
		public void LoadPassable() {
			if (!first) {
				first = true;
				CreatePassable();
			} else {
				loadMarked = true;
			}

			var rooms = Area.Tagged[Tags.Room];
			var f = GetFilling() == Tile.Chasm;
			
			for (var i = 0; i < Size - Width; i++) {
				var found = false;
				var x = FromIndexX(i);
				var y = FromIndexY(i);
				
				foreach (var r in rooms) {
					var room = (Room) r;
					
					if (room.ContainsTile(x, y, 1)) {
						if (!f || room.Type != RoomType.Connection) {
							found = true;
						}

						break;
					}
					
					//if (Get(i).Matches(Tile.WallA, Tile.Transition) && Get(i + Width).Matches(Tile.WallA, Tile.Transition)) {
				}

				if (!found) {
					foreach (var d in Area.Tagged[Tags.Door]) {
						if ((int) Math.Floor(d.CenterX / 16) == x) {
							var yy = (int) Math.Floor(d.CenterY / 16);

							if (yy == y + 1 || yy == y) {
								found = true;
								break;
							}
						}
					}
				}

				if (!found) {
					Explored[i] = true;
					Light[i] = 1f;
				}
			}
		}
		public bool IsPassable(int x, int y, bool chasm = false) {
			return IsPassable(ToIndex(x, y), chasm);
		}
		public bool IsPassable(int i, bool chasm = false) {
			var t = Get(i);

			if (Biome is IceBiome && (t == Tile.WallA || t == Tile.Transition)) {
				return true;
			}

			if (chasm && t == Tile.Chasm) {
				return true;
			}
			
			return t.Matches(TileFlags.Passable) && (Liquid[i] == 0 || Get(i, true).Matches(TileFlags.Passable));
		}
		public void CreatePassable(bool chasm = false) {
			for (var i = 0; i < Size; i++) {
				Passable[i] = IsPassable(i, chasm);
			}
		}
		public void TileUp(bool full = false) {
			cleared = false;
			Size = width * height;
			PathFinder.SetMapSize(Width, Height);
			LevelTiler.TileUp(this, full);
		}
		public void UpdateTile(int x, int y) {
			var i = ToIndex(x, y);
			LevelTiler.TileUp(this, i);

			for (var xx = -3; xx <= 2; xx++) {
				for (var yy = -3; yy <= 2; yy++) {
					var index = ToIndex(xx + x, yy + y);
					
					if (IsInside(index)) {
						Variants[index] = 0;
						LiquidVariants[index] = 0;
						LevelTiler.TileUp(this, index);	
					}
				}
			}
		}
		public void Fill(Tile tile) {
			byte t = (byte) tile;

			for (int i = 0; i < Size; i++) {
				Tiles[i] = t;
			}
		}
		public void Set(int i, Tile value) {
			if (value.Matches(TileFlags.LiquidLayer)) {
				var t = Get(i);
				
				if (t == Tile.Chasm) {
					return;
				}

				if (t.IsWall() || t.Matches(Tile.SensingSpikeTmp) || t.Matches(Tile.SpikeOnTmp) || t.Matches(Tile.SpikeOnTmp) || t.Matches(Tile.Plate)) {
					Tiles[i] = (byte) Tile.FloorA;
					Variants[i] = 0;
				}

				Liquid[i] = (byte) value;
				LiquidVariants[i] = 0;
			} else {
				if (value.IsWall() || value == Tile.Chasm || ((Tile) Liquid[i]).Matches(Tile.Lava, Tile.Rock, Tile.TintedRock, Tile.MetalBlock)) {
					Liquid[i] = 0;
					LiquidVariants[i] = 0;
				}
				
				Tiles[i] = (byte) value;
				Variants[i] = 0;
			}
		}
		public void Set(int x, int y, Tile value) {
			Set(ToIndex(x, y), value);
		}
		public Tile Get(int x, int y, bool liquid = false)
		{
			return Get(ToIndex(x, y), liquid);
		}
		public Tile Get(int i, bool liquid = false) {
			return (Tile) (liquid ? Liquid[i] : Tiles[i]);
		}
		public int ToIndex(int x, int y) {
			return x + y * width;
		}
		public int FromIndexX(int index) {
			return index % width;
		}
		public int FromIndexY(int index) {
			return index / width;
		}
		public bool IsInside(int x, int y) {
			return x >= 0 && y >= 0 && x < width && y < height;
		}
		public bool IsInside(int i) {
			return i >= 0 && i < Size;
		}
		public bool CheckFor(int x, int y, int flag, bool liquid = false) {
			return Get(x, y, liquid).Matches(flag);
		}
		public override void Save(FileWriter stream) {
			base.Save(stream);
			
			stream.WriteString(Biome.Id);
			stream.WriteInt32(width);
			stream.WriteInt32(height);

			for (int i = 0; i < Size; i++) {
				stream.WriteByte(Tiles[i]);
				stream.WriteByte(Liquid[i]);
				stream.WriteByte(Flags[i]);
				stream.WriteBoolean(Explored[i]);
			}	
			
			stream.WriteBoolean(Dark);
			stream.WriteBoolean(Snows);
			stream.WriteBoolean(Rains);

			if (Variant == null) {
				Variant = new RegularLevelVariant();
			}
			
			stream.WriteString(Variant.Id);
		}
		public override void Load(FileReader stream) {
			base.Load(stream);

			var biome = stream.ReadString();

			if (BiomeRegistry.Defined.TryGetValue(biome, out var b)) {
				SetBiome(b);
			} else {
				SetBiome(BiomeRegistry.Defined[Biome.Castle]);
			}
			
			Width = stream.ReadInt32();
			Height = stream.ReadInt32();

			Setup();

			for (var i = 0; i < Size; i++) {
				Tiles[i] = stream.ReadByte();
				Liquid[i] = stream.ReadByte();
				Flags[i] = stream.ReadByte();
				Explored[i] = stream.ReadBoolean();
			}
			
			CreateBody();
			CreateDestroyableBody();
			
			Dark = stream.ReadBoolean();
			Snows = stream.ReadBoolean();
			Rains = stream.ReadBoolean();

			Variant = VariantRegistry.Create(stream.ReadString());
			LoadPassable();
		}
		public void MarkForClearing() {
			Tiles = new byte[Size];
			Liquid = new byte[Size];
			Variants = new byte[Size];
			LiquidVariants = new byte[Size];
			cleared = false;
		}
		public void Setup() {
			Size = width * height;
			
			Tiles = new byte[Size];
			Liquid = new byte[Size];
			Variants = new byte[Size];
			LiquidVariants = new byte[Size];
			Light = new float[Size];
			Flags = new byte[Size];
			WallDecor = new byte[Size];
			Explored = new bool[Size];

			var light = Context.Run.Depth == 0;
			
			for (var i = 0; i < Size; i++) {
				if (Rnd.Chance(10)) {
					WallDecor[i] = (byte) Rnd.Int(1, 9);
				}

				if (light) {
					Explored[i] = true;
					Light[i] = 1;
				}
			}
			
			Passable = new bool[Size];
			MatrixLeak = new bool[Size];

			/*if (Run.Depth > 0) {
				for (var i = 0; i < Size; i++) {
					if (Rnd.Chance(1)) {
						MatrixLeak[i] = true;
					}
				}
			}*/

			PathFinder.SetMapSize(Width, Height);

			// The wall and mess surfaces are render data; a level can be generated without a
			// device, so they are only allocated once a renderer exists.
			if (Graphics.Batch != null) {
				WallSurface = new RenderTarget2D(Engine.GraphicsDevice, Display.Width + 1, Display.Height + 1);
				MessSurface = new RenderTarget2D(Engine.GraphicsDevice, Width * 16, Height * 16, false, Engine.Graphics.PreferredBackBufferFormat, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
			}
		}
		public bool CheckFlag(int x, int y, int i) {
			return CheckFlag(ToIndex(x, y), i);
		}
		public bool CheckFlag(int index, int i) {
			return BitHelper.IsBitSet(Flags[index], i);
		}
		public void SetFlag(int x, int y, int i, bool on) {
			SetFlag(ToIndex(x, y), i, on);
		}
		public void SetFlag(int index, int i, bool on) {
			Flags[index] = (byte) BitHelper.SetBit(Flags[index], i, on);
		}
		public void Break(float x, float y) {
			BSet((int) Math.Floor(x / 16), (int) Math.Floor(y / 16));
			
			BSet((int) Math.Floor(x / 16 - 0.5f), (int) Math.Floor(y / 16));
			BSet((int) Math.Floor(x / 16 + 0.5f), (int) Math.Floor(y / 16));
			BSet((int) Math.Floor(x / 16), (int) Math.Floor(y / 16 - 0.5f));
			BSet((int) Math.Floor(x / 16), (int) Math.Floor(y / 16 + 0.5f));
		}
		private void BSet(int tx, int ty) {
			if (!IsInside(tx, ty)) {
				return;
			}

			var index = ToIndex(tx, ty);
			var tile = (Tile) Tiles[index];

			if (tile != Tile.Planks && (!(Biome is IceBiome) || tile != Tile.WallA)) {
				return;
			}

			Set(index, Tile.FloorA);

			if (tile == Tile.Planks) {
				Set(index, Tile.Ember);
			}

			UpdateTile(tx, ty);
			
			ReCreateBodyChunk(tx, ty);
			Animate(Area, tx, ty);
		}
		public static void Animate(Area area, int x, int y) {
			if (!GameContext.Current.Camera.Overlaps(new Rectangle(x * 16, y * 16, 16, 16))) {
				return;
			}

			area.Add(new TileFx {
				X = x * 16,
				Y = y * 16 - 8
			});
			
			for (var i = 0; i < 3; i++) {
				var part = new ParticleEntity(Particles.Dust());
						
				part.Position = new Vector2(x * 16 + 8, y * 16 + 8);
				area.Add(part);
			}
			
			for (var i = 0; i < 3; i++) {
				var part = new ParticleEntity(Particles.Plank());
						
				part.Position = new Vector2(x * 16 + 8, y * 16);
				part.Particle.Scale = Rnd.Float(0.4f, 0.8f);
				
				area.Add(part);
			}

			GameContext.Current.Camera.Shake(2);
			
			AudioEmitterComponent.Dummy(area, new Vector2(x, y) * 16).EmitRandomizedPrefixed("level_chair_break", 2, 0.75f);
		}
		public void ReTileAndCreateBodyChunks(int x, int y, int w, int h) {
			UpdateTile(x, y);
			ReCreateBodyChunk(x, y);
			
			for (var yy = y - 1; yy < y + h + 1; yy++) {
				for (var xx = x - 1; xx < x + w + 1; xx++) {
					if (yy != y || xx != x) {
						UpdateTile(xx, yy);
						ReCreateBodyChunk(xx, yy);
					}
				}
			}
		}
	}
}
