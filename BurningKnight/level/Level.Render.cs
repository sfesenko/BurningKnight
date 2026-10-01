// #define ART_DEBUG

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
		public override void RenderDebug() {
			Graphics.Batch.DrawRectangle(new RectangleF(0, 0, Width * 16, Height * 16), Color.Green);
		}
		public int GetRenderLeft(Camera camera) {
			return (int) MathUtils.Clamp(0, Width - 1, (int) Math.Floor(camera.X / 16 - 1f));
		}
		public int GetRenderTop(Camera camera) {
			return (int) MathUtils.Clamp(0, Height - 1, (int) Math.Floor(camera.Y / 16 - 1f));
		}
		public int GetRenderRight(Camera camera) {
			return (int) MathUtils.Clamp(0, Width - 1, (int) Math.Ceiling(camera.Right / 16 + 1f));
		}
		public int GetRenderBottom(Camera camera) {
			return (int) MathUtils.Clamp(0, Height - 1, (int) Math.Ceiling(camera.Bottom / 16 + 1f));
		}
		// Renders light emited by tiles
		public void RenderTileLights() {
			if (!LevelLayerDebug.TileLight) {
				return;
			}
			
			var camera = Context.Camera;
			
			// Cache the condition
			var toX = GetRenderRight(camera!);
			var toY = GetRenderTop(camera);

			Graphics.Color = new Color(1f, 1f, 0f, 1f);
			
			for (int y = GetRenderBottom(camera); y >= toY; y--) {
				for (int x = GetRenderLeft(camera); x <= toX; x++) {
					var index = ToIndex(x, y);
					var light = Light[index];

					if (NoLightNoRender && light < LightMin) {
						continue;
					}
					
					var liquid = (Tile) Liquid[index];

					if (liquid == Tile.Lava) {
						Graphics.Render(Tilesets.Biome.Light[LiquidVariants[index]], new Vector2(x * 16 - 24, y * 16 - 24), 0, Vector2.Zero, new Vector2(2));
					}
				}
			}

			Graphics.Color = ColorUtils.WhiteColor;
		}
		// Renders floor layer
		public override void Render() {
			if (this != Context.Level) {
				Done = true;
				return;
			}

			if (!LevelLayerDebug.Floor) {
				return;
			}
			
			manager!.Update();

			var camera = Context.Camera;
			
			// Cache the condition
			var toX = GetRenderRight(camera!);
			var toY = GetRenderTop(camera);
			var active = !Engine.Instance.State.Paused;

			var shader = Shaders.Chasm;
			Shaders.Begin(shader);

			shader.Parameters["h"].SetValue(8f / Tileset!.WallTopA!.Texture!.Height);
			var enabled = shader.Parameters["enabled"];
			enabled.SetValue(false);
							
			for (int y = GetRenderBottom(camera); y >= toY; y--) {
				for (int x = GetRenderLeft(camera); x <= toX; x++) {
					var index = ToIndex(x, y);
					var light = Light[index];

					if (NoLightNoRender && light < LightMin) {
						continue;
					}
					
					var tile = Tiles[index];
					var t = (Tile) tile;

					if (tile > 0) {
						if (t.Matches(TileFlags.FloorLayer)) {
							if (!Settings.LowQuality && active && CheckFlag(index, Flag.Burning) && Rnd.Chance(10)) {
								Area!.Add(new FireParticle {
									Position = new Vector2(x * 16 + Rnd.Float(-2, 18), y * 16 + Rnd.Float(-2, 18)),
									XChange = 0.1f,
									Scale = 0.3f,
									Vy = 8,
									T = 0.5f,
									B = 0
								});
							}
							
							var pos = new Vector2(x * 16, y * 16);

							if (t == Tile.PistonDown) {
								RenderWall(x, y, index, tile, t, 0);
							} else if (t != Tile.Chasm && t != Tile.SpikeOffTmp && t != Tile.SensingSpikeTmp) {
									Graphics.Render((MatrixLeak![index] && t.Matches(Tile.FloorA, Tile.FloorB, Tile.FloorC, Tile.FloorD) ? MatrixTileset : Tileset).Tiles[tile][
#if ART_DEBUG
										0
#else
										Variants[index]
#endif
									], pos);
							}
						}
					}
				}
			}

			Shaders.End(); 
		}
		public void RenderShadows() {
			if (Done) {
				return;
			}
			
			if (!LevelLayerDebug.Shadows) {
				return;
			}
			
			var camera = Context.Camera;

			// Cache the condition
			var toX = GetRenderRight(camera!);
			var toY = GetRenderBottom(camera);

			for (int y = toY; y >= GetRenderTop(camera); y--) {
				for (int x = GetRenderLeft(camera); x <= toX; x++) {
					var index = ToIndex(x, y);
					var tl = (Tile) Tiles[index];
					var tileset = (MatrixLeak[index] ? MatrixTileset : Tileset);

					if (tl.Matches(TileFlags.WallLayer) && (IsInside(index + width))) {
						var t = (Tile) Tiles[index + width];

						if (!t.IsWall() && t != Tile.Chasm) {
							Graphics.Render(tileset!.WallA[CalcWallIndex(x, y)], new Vector2(x * 16, y * 16 + 10), 0, Vector2.Zero,
								Vector2.One, SpriteEffects.FlipVertically);
						}
					}

					if (tl != Tile.Transition && (tl.IsWall() || tl == Tile.PistonDown)) {
						var v = Variants[index];
						var ar = tileset!.WallAExtensions;
						
						switch (tl) {
							case Tile.WallB: {
								ar = tileset.WallBExtensions;
								break;
							}
							
							case Tile.Planks: {
								ar = Tilesets.Biome.PlanksExtensions;
								break;
							}
							
							case Tile.GrannyWall: {
								ar = Tilesets.Biome.GrannyExtensions;
								break;
							}
							
							case Tile.EvilWall: {
								ar = Tilesets.Biome.EvilExtensions;
								break;
							}
						}

						if (!BitHelper.IsBitSet(v, 1)) {
							Graphics.Render(ar[1], new Vector2(x * 16 + 16, y * 16 + 9));
						}

						if (!BitHelper.IsBitSet(v, 2)) {
							Graphics.Render(ar[2], new Vector2(x * 16, y * 16 + 16 + 8));
						}

						if (!BitHelper.IsBitSet(v, 3)) {
							Graphics.Render(ar[3], new Vector2(x * 16 - 8, y * 16 + 9));
						}
					}
					
					var l = Liquid[index];
					var lt = (Tile) l;

					if (lt.IsRock()) {
						Graphics.Render(tileset!.Tiles[l][LiquidVariants[index]], new Vector2(x * 16, y * 16 + 3));
					} else if (lt == Tile.MetalBlock) {
						Graphics.Render(tileset!.MetalBlockShadow, new Vector2(x * 16, y * 16 + 6), 0, Vector2.Zero,
							Vector2.One, SpriteEffects.FlipVertically);
					}
				}
			}
		}
		private void RenderShadowSurface() {
			if (!LevelLayerDebug.Shadows) {
				return;
			}
			
			if (Done) {
				return;
			}
			
			if (Engine.Instance.StateRenderer.UiTarget != null) {
				Graphics.Color = ShadowColor;

				var c = Context.Camera;
				var z = c!.Zoom;
				var n = Math.Abs(z - 1) > 0.01f;
				
				if (n) {
					c.Zoom = 1;
					c.UpdateMatrices();
				}

				Graphics.Render(Engine.Instance.StateRenderer.UiTarget,
					Context.Camera!.TopLeft - new Vector2(Context.Camera!.Position.X % 1, 
						Context.Camera!.Position.Y % 1));

				if (n) {
					c.Zoom = z;
					c.UpdateMatrices();
				}
				
				Graphics.Color = ColorUtils.WhiteColor;
			}
		}
		public void RenderLight() {
			if (!DrawLight || !LevelLayerDebug.TileLight) {
				return;
			}
			
			var camera = Context.Camera;

			// Cache the condition
			var toX = GetRenderRight(camera!);
			var toY = GetRenderBottom(camera);

			var dt = Engine.Delta * 10f;
			var region = Tileset!.WallTopA;
			
			for (int y = GetRenderTop(camera); y <= toY; y++) {
				for (int x = GetRenderLeft(camera); x <= toX; x++) {
					var index = ToIndex(x, y);
					var light = Light[index];

					if (Explored[index] && light < LightMax) {
						Light[index] = light = Math.Min(1, light + dt);
					}

					if (light < LightMax) {
						Graphics.Color.A = (byte) (255 - light * 255);
						Graphics.Render(region, new Vector2(x * 16, y * 16));
					}

					if (Tiles[index] == (byte) Tile.Crack) {
						Graphics.Color.A = 255;
						Graphics.Render(Tileset.WallCrackA, new Vector2(x * 16, y * 16 - 8));
					}
				}
			}
			
			Graphics.Color.A = 255;
		}
	}
}
