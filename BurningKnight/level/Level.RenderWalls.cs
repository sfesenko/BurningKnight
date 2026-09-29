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
		private void RenderSides() {
			if (!LevelLayerDebug.Sides) {
				return;
			}
			
			var camera = Camera.Instance;

			// Cache the condition
			var toX = GetRenderRight(camera);
			var toY = GetRenderBottom(camera);
			
			for (int y = GetRenderTop(camera); y < toY; y++) {
				for (int x = GetRenderLeft(camera); x < toX; x++) {
					var index = ToIndex(x, y);
					var tl = (Tile) Tiles[index];
					
					if (tl.Matches(TileFlags.WallLayer)) {
						if ((IsInside(index + width) && !((Tile) Tiles[index + width]).IsWall())) {
							var pos = new Vector2(x * 16, y * 16 + 8);
							var a = tl.Matches(Tile.WallA, Tile.Piston);
							
							if (tl == Tile.Crack) {
								a = (IsInside(index + 1) && Get(index + 1) == Tile.WallA) ||
								    (IsInside(index + width) && Get(index + width) == Tile.WallA);
							}

							var tileset = (MatrixLeak[index] ? MatrixTileset : Tileset);
							var ar = tileset.WallA;
							var arr = tileset.WallSidesA;

							if (!a) {
								switch (tl) {
									case Tile.Planks: {
										ar = Tilesets.Biome.Planks;
										arr = Tilesets.Biome.PlankSides;
										break;
									}

									case Tile.Crack: {
										if (!(IsInside(index + 1) && Get(index + 1) == Tile.WallA) &&
										    !(IsInside(index + width) && Get(index + width) == Tile.WallA)) {
											
											ar = tileset.WallB;
											arr = tileset.WallSidesB;
										}


										break;
									}
									
									case Tile.WallB: {
										ar = tileset.WallB;
										arr = tileset.WallSidesB;
										break;
									}
																		
									case Tile.EvilWall: {
										ar = Tilesets.Biome.EvilWall;
										arr = Tilesets.Biome.EvilWallSides;
										break;
									}

									case Tile.GrannyWall: {
										ar = Tilesets.Biome.GrannyWall;
										arr = Tilesets.Biome.GrannyWallSides;
										break;
									}
								}
							}
							
							var ind = -1;

							if (index >= Size - 1 || !((Tile) Tiles[index + 1]).Matches(Tile.Piston, Tile.WallA, Tile.WallB,
								    Tile.Planks, Tile.EvilWall, Tile.GrannyWall, Tile.Transition)) {
								ind += 1;
							}

							if (index <= 0 || !((Tile) Tiles[index - 1]).Matches(Tile.Piston, Tile.WallA, Tile.WallB, Tile.EvilWall,
								    Tile.GrannyWall, Tile.Planks, Tile.Transition)) {
								ind += 2;
							}

							if (ind != -1) {
								Graphics.Render(arr[ind], pos);
							} else {
								Graphics.Render(ar[CalcWallIndex(x, y)], pos);
							}
						}
					} else if (tl == Tile.Chasm) {
						if (IsInside(index + width) && !Get(index + width).IsWall() && Get(index + width) != Tile.Chasm) {
							Graphics.Render(Tilesets.Biome.ChasmBottom[CalcChasmIndex(x, y + 1)], new Vector2(x * 16, y * 16 + 16));
						}
								
						if (IsInside(index + 1) && !Get(index + 1).IsWall() && Get(index + 1) != Tile.Chasm) {
							Graphics.Render(Tilesets.Biome.ChasmRight[CalcChasmIndex(x + 1, y)], new Vector2(x * 16 + 16, y * 16));
						}
								
						if (index > 0 && !Get(index - 1).IsWall() && Get(index - 1) != Tile.Chasm) {
							Graphics.Render(Tilesets.Biome.ChasmLeft[CalcChasmIndex(x - 1, y)], new Vector2(x * 16 - 16, y * 16));
						}
					}
				}
			}
		}
		private void RenderWall(int x, int y, int index, int tile, Tile t, int m) {
			var a = false;
			var tileset = (MatrixLeak[index] ? MatrixTileset : Tileset);
			
			if (t != Tile.Transition) {
				var region = t == Tile.Planks ? Tilesets.Biome.PlanksTop : tileset.Tiles[tile][0];
				a = t == Tile.WallA || t == Tile.Piston || t == Tile.PistonDown;
				var ab = a || t == Tile.GrannyWall || t == Tile.EvilWall;
				var effect = Graphics.ParseEffect(x % 2 == 0, y % 2 == 0);

				if (ab) {
					var v = WallDecor[index];

					if (!t.Matches(Tile.Piston, Tile.PistonDown) && v > 0) {
						region = Tileset.WallVariants[v - 1];
					}
				} else if (t == Tile.Crack) {
					ab = (IsInside(index + 1) && Get(index + 1) == Tile.WallA) ||
					     (IsInside(index + width) && Get(index + width) == Tile.WallA);
					region = ab
						? tileset.WallTopA
						: tileset.WallTopB;
				} else {
					effect = SpriteEffects.None;
				}

				Graphics.Render(region, new Vector2(x * 16, y * 16 - 8), 0, Vector2.Zero, Vector2.One, effect);
			}
			
			if (t.IsWall() || t == Tile.PistonDown) {
				byte v = Variants[index];

				for (int xx = 0; xx < 2; xx++) {
					for (int yy = 0; yy < 2; yy++) {
						int lv = 0;

						if (yy > 0 || BitHelper.IsBitSet(v, 0)) {
							lv += 1;
						}

						if (xx == 0 || BitHelper.IsBitSet(v, 1)) {
							lv += 2;
						}

						if (yy == 0 || BitHelper.IsBitSet(v, 2)) {
							lv += 4;
						}

						if (xx > 0 || BitHelper.IsBitSet(v, 3)) {
							lv += 8;
						}

						var ar = tileset.WallTopsA;

						if (!a) {
							switch (t) {
								case Tile.Transition: {
									ar = tileset.WallTopsTransition;
									break;
								}
								
								case Tile.WallB: {
									ar = tileset.WallTopsB;
									break;
								}
								
								case Tile.Crack: {
									if (!(IsInside(index + 1) && Get(index + 1) == Tile.WallA) && !(IsInside(index + width) && Get(index + width) == Tile.WallA)) {
										ar = tileset.WallTopsB;
									}

									break;
								}
								
								case Tile.Planks: {
									ar = Tilesets.Biome.PlankTops;
									break;
								}
								
								case Tile.GrannyWall: {
									ar = Tilesets.Biome.GrannyWallTops;
									break;
								}
								
								case Tile.EvilWall: {
									ar = Tilesets.Biome.EvilWallTops;
									break;
								}
							}
						}
						
						if (lv == 15) {
							lv = 0;

							if (xx == 1 && yy == 0 && !BitHelper.IsBitSet(v, 4)) {
								lv += 1;
							}

							if (xx == 1 && yy == 1 && !BitHelper.IsBitSet(v, 5)) {
								lv += 2;
							}

							if (xx == 0 && yy == 1 && !BitHelper.IsBitSet(v, 6)) {
								lv += 4;
							}

							if (xx == 0 && yy == 0 && !BitHelper.IsBitSet(v, 7)) {
								lv += 8;
							}

							if (lv != 15) {
								var vl = Tileset.WallMapExtra[lv];

								if (vl != -1) {
									var i = ToIndex(x + (xx == 0 ? -1 : 1), y + yy - 1);
									var light = DrawLight ? (i < 0 || i >= Light.Length ? 1 : Light[i]) : 1;

									if (light > LightMin) {
										Graphics.Color.A = (byte) (light * 255);
										var ind = vl + 12 * CalcWallTopIndex(x, y);
										
										Graphics.Render(ar[ind],
											new Vector2(x * 16 + xx * 8, y * 16 + yy * 8 - m));

										Graphics.Color.A = 255;
									}
								}
							}
						} else {
							var vl = Tileset.WallMap[lv];
							
							if (vl != -1) {
								var i = ToIndex(x + (xx == 0 ? -1 : 1), y + yy - 1);
								var light = DrawLight ? (i < 0 || i >= Light.Length ? 1 : Light[i]) : 1;

								if (light > LightMin) {
									Graphics.Color.A = (byte) (light * 255);

									var ind = vl + 12 * CalcWallTopIndex(x, y);
									
									Graphics.Render(ar[ind],
										new Vector2(x * 16 + xx * 8, y * 16 + yy * 8 - m));

									Graphics.Color.A = 255;
								}
							}
						}
					}
				}

				if (t != Tile.Transition) {
					var ar = tileset.WallAExtensions;

					switch (t) {
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
					
					if (!BitHelper.IsBitSet(v, 0)) {
						Graphics.Render(ar[0], new Vector2(x * 16, y * 16 - m - 8));
					}

					if (!BitHelper.IsBitSet(v, 1)) {
						Graphics.Render(ar[1], new Vector2(x * 16 + 16, y * 16 - m));
					}

					if (!BitHelper.IsBitSet(v, 2)) {
						Graphics.Render(ar[2], new Vector2(x * 16, y * 16 - m + 16));
					}

					if (!BitHelper.IsBitSet(v, 3)) {
						Graphics.Render(ar[3], new Vector2(x * 16 - 8, y * 16 - m));
					}
				}
			}
		}
		public void RenderWalls() {
			if (!LevelLayerDebug.Walls) {
				return;
			}
			
			var camera = Camera.Instance;
			var state = (PixelPerfectGameRenderer) Engine.Instance.StateRenderer;
			state.End();
			
			var effect = state.SurfaceEffect;
			state.SurfaceEffect = null;

			Engine.GraphicsDevice.SetRenderTarget(WallSurface);

			Graphics.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, 
				state.RasterizerState, null, Camera.Instance?.Matrix);
			Graphics.Clear(Color.Transparent);

			Graphics.Color = ColorUtils.WhiteColor;
			
			// Cache the condition
			var toX = GetRenderRight(camera);
			var toY = GetRenderBottom(camera);

			for (int y = GetRenderTop(camera); y <= toY; y++) {
				for (int x = GetRenderLeft(camera); x <= toX; x++) {
					var index = ToIndex(x, y);

					var tile = Tiles[index];
					var t = (Tile) tile;

					if (tile > 0 && t.Matches(TileFlags.WallLayer)) {
						RenderWall(x, y, index, tile, t, 8);
					}
				}
			}

			Graphics.Batch.End();
			RenderBlood();
			Graphics.Batch.Begin(SpriteSortMode.Immediate, blend, SamplerState.PointClamp, DepthStencilState.None, 
				state.RasterizerState, null, Camera.Instance?.Matrix);
			
			foreach (var p in Area.Tagged[Tags.Player]) {
				((Player) p).RenderOutline();
			}
			
			Graphics.Batch.End();
			Engine.GraphicsDevice.SetRenderTarget(state.GameTarget);
			
			var c = Camera.Instance;
			var z = c.Zoom;
			var n = Math.Abs(z - 1) > 0.01f;
			
			if (n) {
				c.Zoom = 1;
				c.UpdateMatrices();
			}

			Graphics.Batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
				state.RasterizerState, null, Camera.Instance?.Matrix);
			
			Graphics.Render(WallSurface, Camera.Instance.TopLeft - new Vector2(Camera.Instance.Position.X % 1, 
				                             Camera.Instance.Position.Y % 1));
			
			Graphics.Batch.End();

			if (n) {
				c.Zoom = z;
				c.UpdateMatrices();
			}
			
			Engine.GraphicsDevice.SetRenderTarget(state.GameTarget);
			state.SurfaceEffect = effect;
			state.Begin();
			
			if (!RenderPassable) {
				return;
			}
			
			var color = new Color(1f, 1f, 1f, 0.5f);

			for (int y = GetRenderTop(camera); y <= toY; y++) {
				for (int x = GetRenderLeft(camera); x <= toX; x++) {
					if (Passable[ToIndex(x, y)]) {
						Graphics.Batch.DrawRectangle(new RectangleF(x * 16 + 1, y * 16 + 1, 14, 14), color);
					}
				}
			}
		}
		private byte CalcWallIndex(int x, int y) {
			#if ART_DEBUG
				return 0;
			#else
				return (byte) (((int) Math.Round(x * 3.5f + y * 2.74f)) % 12);
			#endif
		}
		private byte CalcWallSide(int x, int y) {
#if ART_DEBUG
				return 0;
#else
			return (byte) (((int) Math.Round(x * 3.5f + y * 2.74f)) % 4);
#endif
		}
		private byte CalcWallTopIndex(int x, int y) {
#if ART_DEBUG
				return 0;
#else
			return (byte) (((int) Math.Round(x * 16.217f + y * 8.12f)) % 3);
#endif
		}
	}
}
