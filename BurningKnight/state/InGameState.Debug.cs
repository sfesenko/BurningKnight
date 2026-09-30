using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BurningKnight.assets;
using BurningKnight.assets.achievements;
using BurningKnight.assets.input;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.fx;
using BurningKnight.entity.item;
using BurningKnight.entity.item.use;
using BurningKnight.entity.room;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.paintings;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.ui;
using BurningKnight.ui.dialog;
using BurningKnight.ui.inventory;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.entity.component.logic;
using Lens.game;
using Lens.graphics;
using Lens.graphics.gamerenderer;
using Lens.input;
using Lens.lightJson;
using Lens.util;
using Lens.util.camera;
using Lens.services;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Timer = Lens.util.timer.Timer;
using ImGuiNET;
using BurningKnight.ui.editor;
using BurningKnight.ui.imgui;
using Console = BurningKnight.debug.Console;

namespace BurningKnight.state {
	// The dev-tool half of the run state: the console, the editor window and the ImGui pass. A
	// release build excludes every *.Debug.cs (ADR-0003), and the partial hooks above compile out.
	public partial class InGameState {
		private EditorWindow editor;
		public Console Console;

		partial void FocusAreaDebug(Entity entity) {
			AreaDebug.ToFocus = entity;
		}

		partial void UpdateConsole(float dt) {
			Console?.Update(dt);
		}

		partial void RenderEditorInGame() {
			editor?.RenderInGame();
		}

		partial void CreateEditor(Camera camera) {
			if (Assets.ImGuiEnabled) {
				editor = new EditorWindow(new Editor {
					Area = Area,
					Level = Context.Level,
					Camera = camera
				});
			}
		}

		partial void CreateConsole() {
			if (Assets.ImGuiEnabled) {
				Console = new Console(Area);
			}
		}

		public override void RenderNative() {
			if (!Console.Open) {
				return;
			}

			ImGuiHelper.Begin();

			Console?.Render();
			editor?.Render();

			WindowManager.Render(Area);
			ImGuiHelper.End();

			Graphics.Batch.Begin();
			Graphics.Batch.DrawCircle(new CircleF(Mouse.GetState().Position.ToVector2(), 3f), 8, Color.White);
			Graphics.Batch.End();
		}
		partial void UpdateDebug(float dt) {
			if (BK.Version.Dev && Assets.ImGuiEnabled && ((Input.Keyboard.WasPressed(Keys.Tab) && Input.Keyboard.IsDown(Keys.LeftControl)))) {
				ToolsEnabled = !ToolsEnabled;
				var player = LocalPlayer.Locate(Area);

				if (player != null) {
					TextParticle.Add(player, "Dev Tools", 1, true, !ToolsEnabled);
				}
			}
			
			if (!ToolsEnabled) {
				return;
			}
			
			if (Input.Blocked > 0) {
				return;
			}

			if (Input.Keyboard.WasPressed(Keys.Insert)) {
				SaveManager.Delete(SaveType.Game, SaveType.Level, SaveType.Player);
				Context.Run.StartNew(1, Context.Run.Type);
				Died = true;

				Context.Run.NextDepth = Context.Run.Depth;

				return;
			}

			if (Input.Keyboard.IsDown(Keys.LeftControl)) {
				if (Input.Keyboard.WasPressed(Keys.D0)) {
					Context.Run.Depth = 0;
					
					if (Context.Run.Statistics != null) {
						Context.Run.Statistics.Done = true;
						Context.Run.Statistics = null;
					}
				}
				
				if (Input.Keyboard.WasPressed(Keys.D1)) {
					Context.Run.Depth = 1;
				}
				
				if (Input.Keyboard.WasPressed(Keys.D2)) {
					Context.Run.Depth = 3;
				}
				
				if (Input.Keyboard.WasPressed(Keys.D3)) {
					Context.Run.Depth = 5;
				}
				
				if (Input.Keyboard.WasPressed(Keys.D4)) {
					Context.Run.Depth = 7;
				}
				
				if (Input.Keyboard.WasPressed(Keys.D5)) {
					Context.Run.Depth = 9;
				}
				
				if (Input.Keyboard.WasPressed(Keys.D6)) {
					Context.Run.Depth = 11;
				}
			}
			
			if (Input.Keyboard.IsDown(Keys.LeftAlt)) {
				if (Input.Keyboard.WasPressed(Keys.D1)) {
					Context.Run.Depth = 2;
					Player.ToBoss = true;
				}
				
				if (Input.Keyboard.WasPressed(Keys.D2)) {
					Context.Run.Depth = 4;
					Player.ToBoss = true;
				}
				
				if (Input.Keyboard.WasPressed(Keys.D3)) {
					Context.Run.Depth = 6;
					Player.ToBoss = true;
				}
				
				if (Input.Keyboard.WasPressed(Keys.D4)) {
					Context.Run.Depth = 8;
					Player.ToBoss = true;
				}
				
				if (Input.Keyboard.WasPressed(Keys.D5)) {
					Context.Run.Depth = 10;
					Player.ToBoss = true;
				}
				
				if (Input.Keyboard.WasPressed(Keys.D6)) {
					Context.Run.Depth = 11;
					Player.ToBoss = true;
				}
			}

			if (Input.WasPressed(Controls.Fps)) {
				Settings.ShowFps = !Settings.ShowFps;
			}
			
			if (Input.Keyboard.WasPressed(Keys.F3)) {
				Settings.HideUi = !Settings.HideUi;
			}

			if (Input.Keyboard.WasPressed(Keys.F4)) {
				Settings.HideCursor = !Settings.HideCursor;
			}

			if (Input.Keyboard.WasPressed(Keys.F5)) {
				TeleportTo(RoomType.Treasure);
			}
			
			if (Input.Keyboard.WasPressed(Keys.F6)) {
				TeleportTo(RoomType.Shop);
			}
			
			if (Input.Keyboard.WasPressed(Keys.F7)) {
				TeleportTo(RoomType.Special);
			}

			if (Input.Keyboard.WasPressed(Keys.F8)) {
				TeleportTo(RoomType.Secret);
			}

			if (Input.Keyboard.WasPressed(Keys.F9)) {
				TeleportTo(RoomType.Boss);
			}

			if (Input.Keyboard.WasPressed(Keys.NumPad7) || Input.Keyboard.WasPressed(Keys.Home)) {
				var p = LocalPlayer.Locate(Area);
				p.Center = p.GetComponent<CursorComponent>()!.Cursor.GamePosition;
			}

			if (Input.Keyboard.WasPressed(Keys.PageUp)) {
				var level = Context.Level;

				for (var i = 0; i < level.Explored.Length; i++) {
					level.Explored[i] = true;
				}
			}

			if (Input.Keyboard.WasPressed(Keys.NumPad1)) {
				GlobalSave.ResetControlKnowldge();
			}

			if (Input.Keyboard.WasPressed(Keys.PageDown)) {
				Context.Camera!.Detached = !Context.Camera!.Detached;

				if (!Context.Camera!.Detached) {
					ResetFollowing();
				}
			}

			if (Context.Camera!.Detached) {
				float speed = dt * 120f;
				
				if (Input.Keyboard.IsDown(Keys.NumPad4)) {
					Context.Camera!.PositionX -= speed;
				}
				
				if (Input.Keyboard.IsDown(Keys.NumPad6)) {
					Context.Camera!.PositionX += speed;
				}
				
				if (Input.Keyboard.IsDown(Keys.NumPad8)) {
					Context.Camera!.PositionY -= speed;
				}
				
				if (Input.Keyboard.IsDown(Keys.NumPad2)) {
					Context.Camera!.PositionY += speed;
				}
			}
		}
	}
}
