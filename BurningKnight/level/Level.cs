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
	public abstract partial class Level : SaveableEntity {
		public const float LightMin = 0.01f;
		public const float LightMax = 0.95f;
		public static bool RenderPassable = false;
		public static Color ShadowColor = new Color(0f, 0f, 0f, 0.5f);
		public static Color FloorColor = new Color(1f, 1f, 1f, 1f);
		
		public Tileset Tileset;
		public Tileset MatrixTileset;
		public Biome Biome;
		public bool DrawLight = true;
		public bool NoLightNoRender = true;
		public bool Dark;
		public bool Rains;
		public bool Snows;

		public List<string> ItemsToSpawn;

		private int width;
		private int height;
		private float time;

		public new int Width {
			get => width;

			set {
				width = value;
				Size = width * height;
			}
		}
		
		public new int Height {
			get => height;

			set {
				height = value;
				Size = width * height;
			}
		}
		
		public int Size;
		public byte[] Tiles;
		public byte[] Liquid;
		public byte[] Variants;
		public byte[] LiquidVariants;
		public byte[] Flags;
		public byte[] WallDecor;
		public bool[] Explored;
		public bool[] Passable;
		public bool[] MatrixLeak;
		public float[] Light;

		public Chasm Chasm;
		public HalfWall HalfWall;
		public HalfProjectileLevel HalfProjectile;
		public ProjectileLevelBody ProjectileLevelBody;
		public RenderTarget2D WallSurface;
		public RenderTarget2D MessSurface;

		public LevelVariant Variant;

		public Level(BiomeInfo biome) {
			SetBiome(biome);
			
			Run.Level = this;
		}

		public override void Destroy() {
			base.Destroy();

			rainSound?.Stop();
			rainSound?.Dispose();

			if (Chasm != null) {
				HalfProjectile.Done = true;
				Area.Remove(HalfProjectile);
				
				HalfWall.Done = true;
				Area.Remove(HalfWall);

				Chasm.Done = true;
				Area.Remove(Chasm);

				ProjectileLevelBody.Done = true;
				Area.Remove(ProjectileLevelBody);
			}

			if (Run.Level == this) {
				Run.Level = null;
			}

			if (WallSurface != null) {
				WallSurface?.Dispose();
				MessSurface?.Dispose();
			}

			manager?.Destroy();
		}

		public void SetBiome(BiomeInfo biome) {
			if (biome != null) {
				Biome = (Biome) Activator.CreateInstance(biome.Type);
				Tileset = Tilesets.Get(Biome.Tileset);

				if (Tilesets.Biome != null && Tileset != null) {
					Tileset.Tiles[(int) Tile.EvilFloor] = Tilesets.Biome.EvilFloor;
					Tileset.Tiles[(int) Tile.GrannyFloor] = Tilesets.Biome.GrannyFloor;
					Tileset.Tiles[(int) Tile.EvilWall] = Tileset.Tiles[(int) Tile.WallA];
					Tileset.Tiles[(int) Tile.GrannyWall] = Tileset.Tiles[(int) Tile.WallA];
				}

				Biome.Apply();
			}

			if (MatrixTileset == null) {
				MatrixTileset = Tilesets.Get("tech_biome");
			}
		}

		private RenderTriggerManager manager;

		public override void Init() {
			base.Init();
			
			NoLightNoRender = Engine.Instance.State is InGameState;
			Variant = new RegularLevelVariant();
			
			var s = BlendState.AlphaBlend;
				
			blend = new BlendState {
				BlendFactor = s.BlendFactor,
				AlphaDestinationBlend = s.AlphaDestinationBlend,
				ColorDestinationBlend = s.ColorDestinationBlend,
				ColorSourceBlend = s.ColorSourceBlend,
				AlphaBlendFunction = s.AlphaBlendFunction,
				ColorBlendFunction = s.ColorBlendFunction,
					
				AlphaSourceBlend = Blend.DestinationAlpha
			};

			messBlend = new BlendState {
				ColorBlendFunction = BlendFunction.Add,
				ColorSourceBlend = Blend.DestinationColor,
				ColorDestinationBlend = Blend.Zero,
					
				AlphaSourceBlend = Blend.DestinationAlpha
			};

			Depth = Layers.Floor;
		}

		public override void PostInit() {
			base.PostInit();

			manager = new RenderTriggerManager(this);
			
			manager.Add(new RenderTrigger(this, RenderChasms, Layers.Chasm));
			manager.Add(new RenderTrigger(this, RenderLiquids, Layers.Liquid));
			manager.Add(new RenderTrigger(this, RenderSides, Layers.Sides));
			manager.Add(new RenderTrigger(this, RenderWalls, Layers.Wall));
			manager.Add(new RenderTrigger(this, Lights.Render, Layers.Light));
			manager.Add(new RenderTrigger(this, RenderLight, Layers.TileLights));
			manager.Add(new RenderTrigger(this, RenderShadowSurface, Layers.Shadows));
			manager.Add(new RenderTrigger(this, RenderRocks, Layers.Rocks));
		}

		private SoundEffectInstance rainSound;
		
		public void Prepare() {
			try {
				Variant?.PostInit(this);

				if (Dark) {
					Lights.ClearColor = new Color(0f, 0f, 0f, 1f);
				}

				if (Rains) {
					for (var i = 0; i < 40; i++) {
						Run.Level.Area.Add(new RainParticle());
					}

					if (Assets.LoadSfx) {
						var sound = "level_rain_regular";

						if (Biome is IceBiome) {
							sound = "level_rain_snow";
						} else if (Biome is JungleBiome) {
							sound = "level_rain_jungle";
						}

						var s = Audio.GetSfx(sound);

						if (s != null) {
							rainSound = s.CreateInstance();

							if (rainSound != null) {
								rainSound.Volume = 0;
								rainSound.IsLooped = true;
								rainSound.Play();

								Tween.To(0.5f * Settings.MusicVolume * Settings.MasterVolume, 0, x => rainSound.Volume = x, 0.5f)
									.Delay = 3f;
							}
						}
					}
				}

				if (Snows) {
					for (var i = 0; i < 120; i++) {
						Run.Level.Area.Add(new SnowParticle());
					}
				}
			} catch (Exception e) {
				Log.Error(e);
			}

			TileUp();
		}

		public override void AddComponents() {
			base.AddComponents();
			
			AddComponent(new LevelBodyComponent {
				Level = this
			});
			
			AddComponent(new ShadowComponent(RenderShadows));

			AlwaysActive = true;
			AlwaysVisible = true;
		}

		public void UpdateRainVolume() {
			if (rainSound != null) {
				rainSound.Volume = (Player.InBuilding ? 0.1f : 0.5f) * Settings.MusicVolume * Settings.MasterVolume;
			}
		}

		private BlendState blend;
		private BlendState messBlend;

		public override void Update(float dt) {
			base.Update(dt);
			
			if (loadMarked) {
				loadMarked = false;
				CreatePassable();
			}
			
			time += dt;
		}
		
		public virtual Tile GetFilling() {
			return Biome.GetFilling();
		}

		public virtual int GetPadding() {
			return 1;
		}

		private void ResizeArray<T>(ref T[] array, int w, int h, int newSize, T val) {
			var newArray = new T[newSize];

			for (var y = 0; y < h; y++) {
				for (var x = 0; x < w; x++) {
					newArray[x + y * w] = x >= Width || y >= Height ? val : array[x + y * Width];
				}
			}

			array = newArray;
		}

		public void Resize(int w, int h) {
			var size = w * h;
			
			ResizeArray(ref Tiles, w, h, size, (byte) Tile.FloorA);
			ResizeArray(ref Liquid, w, h, size, (byte) 0);
			ResizeArray(ref Variants, w, h, size, (byte) 0);
			ResizeArray(ref LiquidVariants, w, h, size, (byte) 0);
			ResizeArray(ref Flags, w, h, size, (byte) 0);
			ResizeArray(ref Explored, w, h, size, false);
			ResizeArray(ref Passable, w, h, size, false);
			ResizeArray(ref MatrixLeak, w, h, size, false);
			ResizeArray(ref Light, w, h, size, 0);

			Width = w;
			Height = h;
			Size = size;
			cleared = false;
			
			PathFinder.SetMapSize(Width, Height);

			RefreshSurfaces();
		}

		public void RefreshSurfaces() {
			WallSurface.Dispose();
			MessSurface.Dispose();
			cleared = false;

			WallSurface = new RenderTarget2D(Engine.GraphicsDevice, Display.Width + 1, Display.Height + 1);

			MessSurface = new RenderTarget2D(Engine.GraphicsDevice, Width * 16, Height * 16, false,
				Engine.Graphics.PreferredBackBufferFormat, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
		}

		public virtual string GetMusic() {
			return Biome.GetMusic();
		}

		public static string GetDepthString(bool eng = false) {
			if (Run.Depth < 1) {
				return Locale.Get(Run.Level.Biome.Id, eng);
			}

			var s = $"{Locale.Get(Run.Level.Biome.Id, eng)} {MathUtils.ToRoman((Run.Depth - 1) % 2 + 1)}";

			if (Run.Loop > 0) {
				s = $"L{Run.Loop} {s}";
			}
			
			return s;
		}
	}
}