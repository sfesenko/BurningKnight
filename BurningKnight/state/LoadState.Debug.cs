using ImGuiNET;
using BurningKnight.debug;
using BurningKnight.ui.imgui;
using BurningKnight.assets;
using Lens.assets;
using Console = BurningKnight.debug.Console;

namespace BurningKnight.state {
	public partial class LoadState {
		// The overlay pass; a release build excludes every *.Debug.cs (ADR-0003).
		public override void RenderNative() {
			if (!Assets.ImGuiEnabled) {
				return;
			}

			ImGuiHelper.Begin();

			if (Console.Open) {
				DebugWindow.Render();
			}

			ImGuiHelper.End();
		}
	}
}
