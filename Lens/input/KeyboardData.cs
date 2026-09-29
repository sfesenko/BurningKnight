#if DEBUG
using ImGuiNET;
#endif
using Lens.assets;
using Microsoft.Xna.Framework.Input;

namespace Lens.input {
	public class KeyboardData {
		public KeyboardState PreviousState;
		public KeyboardState State;
		
		private bool guiBlocksKeyboard;

		public KeyboardData() {
			State = Keyboard.GetState();
		}
		
		public void Update() {
			PreviousState = State;
			State = Keyboard.GetState();
			
			// The overlay is a development tool; a release build has no ImGui to ask (ADR-0003).
#if DEBUG
			guiBlocksKeyboard = Assets.ImGuiEnabled && Input.EnableImGuiFocus && ImGui.GetIO().WantCaptureKeyboard;
#endif
		}

		public bool Check(Keys key, Input.CheckType type, bool ignoreGui = false) {
			switch (type) {
				case Input.CheckType.PRESSED: return WasPressed(key, ignoreGui);
				case Input.CheckType.RELEASED: return WasReleased(key, ignoreGui);
				case Input.CheckType.DOWN: return IsDown(key, ignoreGui);
			}

			return false;
		}

		public bool IsDown(Keys key, bool ignoreGui = false) {
			return (ignoreGui || !guiBlocksKeyboard) && State.IsKeyDown(key);
		}

		public bool WasPressed(Keys key, bool ignoreGui = false) {
			return (ignoreGui || !guiBlocksKeyboard) && State.IsKeyDown(key) && !PreviousState.IsKeyDown(key);
		}
		
		public bool WasReleased(Keys key, bool ignoreGui = false) {
			return (ignoreGui || !guiBlocksKeyboard) && !State.IsKeyDown(key) && PreviousState.IsKeyDown(key);
		}
	}
}