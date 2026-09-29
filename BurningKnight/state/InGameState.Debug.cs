using ImGuiNET;
using BurningKnight.assets;
using Lens.entity;
using BurningKnight.ui.editor;
using BurningKnight.ui.imgui;
using Lens.assets;
using Lens.graphics;
using Lens.util.camera;
using Lens.input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
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
					Level = Run.Level,
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
	}
}
