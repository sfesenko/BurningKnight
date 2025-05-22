using System;
using System.Collections.Generic;
using BurningKnight.assets;
using BurningKnight.level.biome;
using BurningKnight.level.tile;
using BurningKnight.state;
using BurningKnight.ui.editor.command;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.graphics;
using Lens.input;
using Lens.util.camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Num = System.Numerics;

namespace BurningKnight.ui.editor {
	public static class TileEditor {
		private static readonly Num.Vector2 TileSize = new(32f);
		private static readonly Num.Vector4 TintColorActive = new(0.6f);
		private static readonly Num.Vector4 TintColor = new(1f);
		private static readonly Num.Vector4 Bg = new(0.1f);

		public static Editor Editor;
		public static EditorWindow Window;
		
		private static string[] _biomes;
		private static int _currentBiome;
		private static readonly List<TileInfo> Infos = [];
		private static Texture2D _biomeTexture;
		private static IntPtr _biomePointer;
		private static Texture2D _tilesetTexture;
		private static IntPtr _tilesetPointer;
		private static bool _fill;
		private static bool _open;

		private static TileInfo CurrentInfo;
		private static bool Grid;
		
		public static void ReloadBiome() {
			if (Editor?.Level?.Biome == null) {
				return;
			}
		
			_biomes = new string[BiomeRegistry.Defined.Count];
			var i = 0;
			
			foreach (var r in BiomeRegistry.Defined.Values) {
				if (r.Id == Editor?.Level?.Biome?.Id) {
					_currentBiome = i;
				}
				
				_biomes[i] = r.Id;
				i++;
			}
			
			_tilesetTexture = Animations.Get($"{Editor.Level.Biome.Id}_biome").Texture;
			_tilesetPointer = ImGuiHelper.Renderer.BindTexture(_tilesetTexture);
			
			_biomeTexture = Animations.Get("biome_assets").Texture;
			_biomePointer = ImGuiHelper.Renderer.BindTexture(_biomeTexture);
			
			Infos.Clear();
			
			DefineTile(Tile.WallA, 128, 0);
			DefineTile(Tile.WallB, 144, 0);
			DefineTile(Tile.Planks, 352, 144, true);
			DefineTile(Tile.Collider, 305, 96, true);
			DefineTile(Tile.GrannyWall, 192, 336, true);
			DefineTile(Tile.EvilWall, 0, 336, true);
			DefineTile(Tile.Crack, 128, 48);
			DefineTile(Tile.GrannyFloor, 192, 368, true);
			DefineTile(Tile.EvilFloor, 48, 384, true);
			DefineTile(Tile.FloorA, 0, 80);
			DefineTile(Tile.FloorB, 64, 80);
			DefineTile(Tile.FloorC, 0, 160);
			DefineTile(Tile.FloorD, 64, 160);
			DefineTile(Tile.Path, 400, 368, true);
			DefineTile(Tile.GrannyFloor, 192, 368);
			DefineTile(Tile.EvilFloor, 48, 384);
			DefineTile(Tile.Water, 64, 240, true);
			DefineTile(Tile.Ice, 192, 112, true);
			DefineTile(Tile.Lava, 64, 112, true);
			DefineTile(Tile.Venom, 64, 304, true);
			DefineTile(Tile.Obsidian, 64, 176, true);
			DefineTile(Tile.Dirt, 64, 48, true);
			DefineTile(Tile.Sand, 464, 352, true);
			DefineTile(Tile.Snow, 464, 288, true);
			DefineTile(Tile.Grass, 192, 48, true);
			DefineTile(Tile.HighGrass, 336, 0, true);
			DefineTile(Tile.Cobweb, 192, 240, true);
			DefineTile(Tile.Ember, 144, 160, true);
			DefineTile(Tile.Chasm, 288, 32, true);
			DefineTile(Tile.Piston, 128, 0);
			DefineTile(Tile.PistonDown, 128, 0);
			DefineTile(Tile.Rock, 160, 192);
			DefineTile(Tile.TintedRock, 160, 224);
			DefineTile(Tile.MetalBlock, 128, 192);
			
			CurrentInfo = Infos[0];
		}
		
		public static void Render() {
			if (!ImGui.Begin("Tile editor", ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.End();
				_open = false;
				return;
			}

			_open = true;

			if (ImGui.Combo("Biome", ref _currentBiome, _biomes, _biomes.Length)) {
				Editor.Level.SetBiome(BiomeRegistry.Get(_biomes[_currentBiome]));
				ReloadBiome();
			}
			
			ImGui.Checkbox("Show grid", ref Grid);
			
			EntityEditor.RemoveEntity();
			
			var down = !ImGui.GetIO().WantCaptureMouse && Input.Mouse.CheckLeftButton;
			var clicked = !ImGui.GetIO().WantCaptureMouse && MouseData.HadClick;
				
			ImGui.Checkbox("Fill", ref _fill);
			ImGui.Separator();

			CurrentInfo ??= Infos[1];

			var cur = CurrentInfo;

			// 4
			ImGui.ImageButton(cur.ToString(), cur.Texture, TileSize, cur.Uv0, cur.Uv1, Bg, TintColor);
			ImGui.SameLine();
			ImGui.Text(CurrentInfo.Tile.ToString());

			if (CurrentInfo.Tile.Matches(TileFlags.LiquidLayer)) {
				ImGui.SameLine();
				ImGui.Text("Liquid");
			} else if (CurrentInfo.Tile.Matches(TileFlags.WallLayer)) {
				ImGui.SameLine();
				ImGui.Text("Wall");
			}
					
			if (Input.Keyboard.WasPressed(Keys.F)) {
				_fill = true;
			} else if (Input.Keyboard.WasPressed(Keys.P)) {
				_fill = false;
			}

			if (CurrentInfo.Tile.Matches(TileFlags.Burns)) {
				ImGui.SameLine();
				ImGui.Text("Burns");
			}
				
			ImGui.Separator();
			for (var i = 0; i < Infos.Count; i++) {
				var info = Infos[i];
				ImGui.PushID((int) info.Tile);
				var active = info == CurrentInfo ? TintColorActive : TintColor;
				if (ImGui.ImageButton(info.ToString(), info.Texture, TileSize, info.Uv0, info.Uv1,  Bg, active)) {
					CurrentInfo = info;
				}

				ImGui.PopID();
				
				if (i % 6 < 5 && i < Infos.Count - 1) {
					ImGui.SameLine();
				}
			}
			
			if (down)
			{
				PlaceTile(CurrentInfo.Tile);
			}
			
			ImGui.End();
		}

		private static void PlaceTile(Tile tile)
		{
			var mouse = Input.Mouse.GamePosition;

			var x = (int) (mouse.X / 16);
			var y = (int) (mouse.Y / 16);

			if (Editor.Level.IsInside(x, y) && Editor.Level.Get(x, y, tile.Matches(TileFlags.LiquidLayer)) != tile)
			{
				Command command = _fill
						? new FillCommand { X = x, Y = y, Tile = tile }
						: new SetCommand { X = x, Y = y, Tile = tile }
					;
				Window.Commands.Do(command);
			}
		}

		private static void DefineTile(Tile tile, int x, int y, bool biome = false)
		{
			var texture2D = biome ? _biomeTexture : _tilesetTexture;
			var pointer = biome ? _biomePointer : _tilesetPointer;
			Infos.Add(new TileInfo(tile, texture2D, pointer, x, y));
		}

		public static void RenderInGame() {
			if (!_open) {
				return;
			}
			
			Color color;

			if (Grid) {
				const int gridSize = 16;
				var off = (Camera.Instance.TopLeft - new Vector2(0, 8));
				color = new Color(1f, 1f, 1f, 0.5f);

				for (var x = Math.Max(0, off.X - off.X % gridSize); x <= off.X + Display.Width && x <= Editor.Level.Width * 16; x += gridSize) {
					Graphics.Batch.DrawLine(x, off.Y, x, off.Y + Display.Height + gridSize, color);
				}

				for (var y = Math.Max(0, off.Y - off.Y % gridSize); y <= off.Y + Display.Height && y <= Editor.Level.Height * 16; y += gridSize) {
					Graphics.Batch.DrawLine(off.X, y, off.X + Display.Width + gridSize, y, color);
				}
			}
			
			var mouse = Input.Mouse.GamePosition;
			color = new Color(1f, 0.5f, 0.5f, 1f);
			var fill = new Color(1f, 0.5f, 0.5f, 0.5f);

			mouse.X = (float) (Math.Floor(mouse.X / 16) * 16);
			mouse.Y = (float) (Math.Floor(mouse.Y / 16) * 16);

			if (CurrentInfo.Tile.Matches(TileFlags.WallLayer)) {
				mouse.Y -= 8;
				Graphics.Batch.FillRectangle(mouse, new Vector2(16, 24), fill);
				mouse.Y += 16;
				Graphics.Batch.DrawRectangle(mouse, new Vector2(16, 8), new Color(1f, 0.7f, 0.7f, 1f));
				mouse.Y -= 16;
				Graphics.Batch.DrawRectangle(mouse, new Vector2(16), color);
			} else {
				Graphics.Batch.FillRectangle(mouse, new Vector2(16, 16), fill);
				Graphics.Batch.DrawRectangle(mouse, new Vector2(16), color);
			}
		}
	}
}