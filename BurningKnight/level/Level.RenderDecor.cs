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
		private bool cleared;
		public void RenderMess() {
			if (!LevelLayerDebug.Mess ) {
				return;
			}
			
			var camera = Context.Camera;
			var state = Engine.Instance.StateRenderer;
			state.End();

			Engine.GraphicsDevice.SetRenderTarget(MessSurface);
			Graphics.Batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, 
				state.ClipRasterizerState, null, Matrix.Identity);

			if (!cleared) {
				cleared = true;
				Graphics.Clear(Color.White);
				// Uncomment for a wild graphics effect
			}/* else {
				Graphics.Clear(new Color(1f, 1f, 1f, 1 / 255f));
			}*/

			Graphics.Color = ColorUtils.WhiteColor;
			
			foreach (var p in Area!.Tagged[Tags.Mess]) {
				((SplashFx) p).RenderInSurface();
			}
			
			Graphics.Color = ColorUtils.WhiteColor;

			Graphics.Batch.End();
			Engine.GraphicsDevice.SetRenderTarget(state.GameTarget);
			Graphics.Batch.Begin(SpriteSortMode.Immediate, messBlend, SamplerState.PointClamp, DepthStencilState.None, 
				state.ClipRasterizerState, null, Context.Camera?.Matrix);
			
			var region = new TextureRegion();

			region.Texture = MessSurface;
			region.Source.X = (int) Math.Floor(camera!.X);
			region.Source.Y = (int) Math.Floor(camera.Y);
			region.Source.Width = Display.Width + 1;
			region.Source.Height = Display.Height + 1;
			
			Graphics.Color = FloorColor;

			Graphics.Render(region, camera.TopLeft - new Vector2(camera.Position.X % 1, 
				                        camera.Position.Y % 1));
			Graphics.Color = ColorUtils.WhiteColor;
			
			Graphics.Batch.End();
			Engine.GraphicsDevice.SetRenderTarget(state.GameTarget);
			state.Begin();
		}
		public void RenderRocks() {
			if (!LevelLayerDebug.Rocks) {
				return;
			}

			var camera = Context.Camera;

			// Cache the condition
			var toX = GetRenderRight(camera!);
			var toY = GetRenderBottom(camera);

			for (int y = toY; y >= GetRenderTop(camera); y--) {
				for (int x = GetRenderLeft(camera); x <= toX; x++) {
					var index = ToIndex(x, y);
					var light = Light[index];

					if (NoLightNoRender && light < LightMin) {
						continue;
					}
					
					var tile = Liquid[index];

					if (tile > 0) {
						var tt = (Tile) tile;

						if (tt.IsHalfWall()) {
							Graphics.Render(Tileset!.Tiles[tile][LiquidVariants[index]], new Vector2(x * 16, y * 16));
						}
					}
				}
			}
		}
		public void RenderLiquids() {
			if (!LevelLayerDebug.Liquids) {
				return;
			}

			var camera = Context.Camera;

			// Cache the condition
			var toX = GetRenderRight(camera!);
			var toY = GetRenderBottom(camera);

			var region = new TextureRegion();
			var shader = Shaders.Terrain;
						
			Shaders.Begin(shader);

			var paused = Engine.Instance.State.Paused;
			
			var enabled = shader.Parameters["enabled"];
			var tilePosition = shader.Parameters["tilePosition"];
			var edgePosition = shader.Parameters["edgePosition"];
			var flow = shader.Parameters["flow"];
			flow.SetValue(0f);
			
			shader.Parameters["time"].SetValue(time * 0.04f);
			shader.Parameters["h"].SetValue(64f / Tilesets.Biome.WaterPattern!.Texture!.Height);

			var sy = shader.Parameters["sy"];

			enabled.SetValue(true);

			for (int y = toY; y >= GetRenderTop(camera); y--) {
				for (int x = GetRenderLeft(camera); x <= toX; x++) {
					var index = ToIndex(x, y);
					var light = Light[index];

					if (NoLightNoRender && light < LightMin) {
						continue;
					}
					
					var tile = Liquid[index];

					if (tile > 0) {
						var tt = (Tile) tile;

						if (tt == Tile.Collider) {
							#if DEBUG
							if (Engine.EditingLevel) {
								enabled.SetValue(false);
								Graphics.Render(Tilesets.Biome.Collider, new Vector2(x * 16, y * 16));
								enabled.SetValue(true);
							}
							#endif
							
							continue;
						}

						if (tt.IsHalfWall()) {
							continue;
						}
						
						region.Set(Tilesets.Biome.Patterns[tile]);
						region.Source.Width = 16;
						region.Source.Height = 16;

						if (tt == Tile.Water) {
							flow.SetValue(1f);
							sy.SetValue(y % 4 * 16f / Tilesets.Biome.WaterPattern.Texture.Height);
						} else if (tt == Tile.Lava) {
							flow.SetValue(0.25f);
							sy.SetValue(y % 4 * 16f / Tilesets.Biome.WaterPattern.Texture.Height);
						} else {
							region.Source.Y += y % 4 * 16;
						}

						region.Source.X += x % 4 * 16;
						
						var pos = new Vector2(x * 16, y * 16);
						var t = (Tile) tile;
						
						if (!t.Matches(Tile.Ember, Tile.Chasm)) {
							var edge = Tilesets.Biome.Edges[tile][LiquidVariants[index]];

							edgePosition.SetValue(new Vector2(
								(float) edge.Source.X / edge!.Texture!.Width,
								(float) edge.Source.Y / edge.Texture.Height
							));
							
							tilePosition.SetValue(new Vector2(
								(float) region.Source.X / region!.Texture!.Width,
								(float) region.Source.Y / region.Texture.Height
							));
							
							Graphics.Render(region, pos);

							if ((t == Tile.Water || t == Tile.Lava) && !Settings.LowQuality && !paused) {
								if (t == Tile.Lava && Rnd.Chance(0.5f)) {
									var p = Particles.Wrap(Particles.Lava(), Area!, pos + Rnd.Vector(0, 16));
									p.Particle.Velocity = MathUtils.CreateVector(Rnd.Float(-10, 10), -Rnd.Float(30, 45));
									p.Particle.Scale = Rnd.Float(0.3f, 0.5f);
									p.Particle.T = 0;
								}
								
								if (Get(index + width) == Tile.Chasm && Rnd.Chance(6)) {
									Area!.Add(new WaterfallFx {
										Position = pos + new Vector2(Rnd.Float(16), 16),
										Lava = t == Tile.Lava
									});
								}
							}
						} else {
							enabled.SetValue(false);
							Graphics.Render(region, pos);
							enabled.SetValue(true);
						}
						
						if (tt == Tile.Water || tt == Tile.Lava) {
							flow.SetValue(0f);
						}
					}
				}
			}
			
			Shaders.End();
			RenderMess();
		}
		private TextureRegion? clear;
		private void RenderChasms() {
			if (!LevelLayerDebug.Chasms) {
				return;
			}
			
			var camera = Context.Camera;

			// Cache the condition
			var toX = GetRenderRight(camera!);
			var toY = GetRenderBottom(camera);
			
			var active = !Engine.Instance.State.Paused;
			var state = Engine.Instance.StateRenderer;
			state.End();

			if (clear == null) {
				clear = CommonAse.Particles.GetSlice("wall");
			}

			Engine.GraphicsDevice.SetRenderTarget(MessSurface);
			Graphics.Batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, 
				state.ClipRasterizerState, null, Matrix.Identity);

			for (int y = GetRenderTop(camera); y < toY; y++) {
				for (int x = GetRenderLeft(camera); x < toX; x++) {
					if ((Tile) Tiles[ToIndex(x, y)] == Tile.Chasm) {
						Graphics.Render(clear!, new Vector2(x * 16, y * 16));
					}
				}
			}

			Graphics.Batch.End();
			Engine.GraphicsDevice.SetRenderTarget(state.GameTarget);
			var shader = Shaders.Chasm;
			Graphics.Batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, 
					state.ClipRasterizerState, shader, Context.Camera?.Matrix);

			shader.Parameters["h"].SetValue(8f / Tileset!.WallTopA!.Texture!.Height);
			var sy = shader.Parameters["y"];
			var enabled = shader.Parameters["enabled"];
			enabled.SetValue(true);

			for (int y = GetRenderTop(camera); y < toY; y++) {
				for (int x = GetRenderLeft(camera); x < toX; x++) {
					var index = ToIndex(x, y);

					if ((Tile) Tiles[index] == Tile.Chasm) {
						var pos = new Vector2(x * 16, y * 16);

						if (active && Rnd.Chance(0.1f)) {
							Area!.Add(new ChasmFx {
								Position = pos + new Vector2(Rnd.Float(16), Rnd.Float(16))
							});
						}

						if (index >= width) {
							var tt = Get(index - width);

							if (tt != Tile.Chasm) {
								var ind = CalcWallSide(x, y);
								/*var id = -1;

								if (!IsInside(index + 1 - width) || ((Tile) Tiles[index + 1 - width]).Matches(Tile.Chasm)) {
									id += 1;
								}
					
								if (!IsInside(index - 1 - width) || ((Tile) Tiles[index - 1 - width]).Matches(Tile.Chasm)) {
									id += 2;
								}*/

								var tileset = (MatrixLeak[index] ? MatrixTileset : Tileset);
								TextureRegion textureRegion;
							
								switch (tt) {
									case Tile.WallA: case Tile.Piston: case Tile.PistonDown:
										textureRegion = tileset!.WallA[ind];
										break;
									case Tile.Planks:
										textureRegion = Tilesets.Biome.Planks[ind];
										break;
									case Tile.EvilWall: case Tile.EvilFloor:
										textureRegion = Tilesets.Biome.EvilWall[ind];
										break;
									case Tile.GrannyWall: case Tile.GrannyFloor:
										textureRegion = Tilesets.Biome.GrannyWall[ind];
										break;
									case Tile.FloorA:
										textureRegion = tileset!.FloorSidesA[ind];
										break;
									case Tile.FloorB:
										textureRegion = tileset!.FloorSidesB[ind];
										break;
									case Tile.FloorC:
										textureRegion = tileset!.FloorSidesC[ind];
										break;
									case Tile.FloorD:
										textureRegion = tileset!.FloorSidesD[ind];
										break;

									default:
									case Tile.WallB:
										textureRegion = tileset!.WallB[ind];
										break;
								}
								
								sy.SetValue((float) textureRegion.Source.Y / textureRegion!.Texture!.Height);
								Graphics.Render(textureRegion, pos);
							}
						}
					}
				}
			}
			
			enabled.SetValue(false);
			Shaders.End();
		}
		public void RenderBlood() {
			if (!LevelLayerDebug.Blood) {
				return;
			}
			
			if (MessSurface == null) {
				return;
			}
			
			var camera = Context.Camera;
			var state = Engine.Instance.StateRenderer;

			Graphics.Batch.Begin(SpriteSortMode.Immediate, messBlend, SamplerState.PointClamp, DepthStencilState.None, 
				state.ClipRasterizerState, null, Context.Camera?.Matrix);
			
			var region = new TextureRegion();

			region.Texture = MessSurface;
			region.Source.X = (int) Math.Floor(camera!.X);
			region.Source.Y = (int) Math.Floor(camera.Y) + 8;
			region.Source.Width = Display.Width + 1;
			region.Source.Height = Display.Height + 1;
			
			Graphics.Render(region, camera.TopLeft - new Vector2(camera.Position.X % 1, 
				                        camera.Position.Y % 1));
			
			Graphics.Batch.End();
		}
		private byte CalcChasmIndex(int x, int y) {
#if ART_DEBUG
				return 0;
#else
			return (byte) (((int) Math.Round(x * 7.417f + y * 2.12f)) % 3);
#endif
		}
	}
}
