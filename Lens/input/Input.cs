using System.Collections.Generic;
using Lens.util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Lens.input {
	public static class Input {
		public enum CheckType {
			PRESSED,
			RELEASED,
			DOWN
		}

		public static bool EnableImGuiFocus;

		public static KeyboardData Keyboard = null!; // Init() sets them
		public static MouseData Mouse = null!;
		public static GamepadData[] Gamepads = null!;
		public static int Blocked;

		private static Dictionary<string, InputButton> Buttons = new Dictionary<string, InputButton>();

		public static void Init() {
			Keyboard = new KeyboardData();
			Mouse = new MouseData();
			Gamepads = new GamepadData[4];

			for (int i = 0; i < Gamepads.Length; i++) {
				Gamepads[i] = new GamepadData((PlayerIndex) i);
			}
		}

		public static void Destroy() {
			foreach (var gamepad in Gamepads) {
				gamepad.StopRumble();
			}
		}

		public static void Update(float dt) {
			GamepadData.WasChanged = false;

			Keyboard.Update();
			Mouse.Update();

			foreach (var gamepad in Gamepads) {
				gamepad.Update(dt);
			}
		}

		public static void ClearBindings() {
			Buttons.Clear();
		}

		public static void Bind(string id, params Keys[] values) {
			var button = Buttons.ContainsKey(id) ? Buttons[id] : new InputButton();

			if (button.Keys == null) {
				button.Keys = new List<Keys>();
			}

			foreach (var key in values) {
				button.Keys.Add(key);
			}

			Buttons[id] = button;
		}

		public static void Bind(string id, params Buttons[] values) {
			var button = Buttons.ContainsKey(id) ? Buttons[id] : new InputButton();

			if (button.Buttons == null) {
				button.Buttons = new List<Buttons>();
			}

			foreach (var b in values) {
				button.Buttons.Add(b);
			}

			Buttons[id] = button;
		}

		public static void Bind(string id, params MouseButtons[] values) {
			var button = Buttons.ContainsKey(id) ? Buttons[id] : new InputButton();

			if (button.MouseButtons == null) {
				button.MouseButtons = new List<MouseButtons>();
			}

			foreach (var b in values) {
				button.MouseButtons.Add(b);
			}

			Buttons[id] = button;
		}

		// Single core behind both Check overloads: guard + binding lookup run once, no allocations.
		private static bool CheckCore(string id, CheckType type, bool ignoreBlock, bool keyboardEnabled,
			GamepadData? gamepad, bool anyPadFallback, bool mouseEnabled) {
			if (Blocked > 0 && !ignoreBlock) {
				return false;
			}

			if (!Buttons.TryGetValue(id, out var button)) {
				return false;
			}

			if (keyboardEnabled && button.Keys != null) {
				foreach (var key in button.Keys) {
					if (Keyboard.Check(key, type)) {
						return true;
					}
				}
			}

			if (button.Buttons != null) {
				if (gamepad != null) {
					if (gamepad.Attached) {
						foreach (var b in button.Buttons) {
							if (gamepad.Check(b, type)) {
								return true;
							}
						}
					}
				} else if (anyPadFallback) {
					// No controller named (cutscene, menu before the player exists): any attached
					// pad answers — a handheld with no keyboard could never get past those screens.
					foreach (var attached in Gamepads) {
						if (!attached.Attached) {
							continue;
						}

						foreach (var b in button.Buttons) {
							if (attached.Check(b, type)) {
								return true;
							}
						}
					}
				}
			}

			if (mouseEnabled && button.MouseButtons != null) {
				foreach (var b in button.MouseButtons) {
					if (Mouse.Check(b, type)) {
						return true;
					}
				}
			}

			return false;
		}

		private static bool Check(string id, CheckType type, GamepadData? data = null, bool ignoreBlock = false) {
			return CheckCore(id, type, ignoreBlock, true, data, data == null, true);
		}

		private static bool Check(string id, CheckType type, InputComponent data, bool ignoreBlock = false) {
			return CheckCore(id, type, ignoreBlock, data.KeyboardEnabled,
				data.GamepadEnabled ? data.GamepadData : null, false, data.KeyboardEnabled);
		}

		public static bool IsDownOnController(string id, GamepadData data) {
			if (!Buttons.TryGetValue(id, out var button)) {
				return false;
			}
			
			if (data != null && data.Attached && button.Buttons != null) {
				foreach (var b in button.Buttons) {
					if (data.IsDown(b)) {
						return true;
					}
				}
			}
			
			return false;
		}

		public static bool WasPressed(string id, GamepadData? data = null, bool ignoreBlock = false) {
			return Check(id, CheckType.PRESSED, data, ignoreBlock);
		}

		public static bool WasReleased(string id, GamepadData? data = null, bool ignoreBlock = false) {
			return Check(id, CheckType.RELEASED, data, ignoreBlock);
		}

		public static bool IsDown(string id, GamepadData? data = null, bool ignoreBlock = false) {
			return Check(id, CheckType.DOWN, data, ignoreBlock);
		}

		public static bool WasPressed(string id, InputComponent data, bool ignoreBlock = false) {
			return Check(id, CheckType.PRESSED, data, ignoreBlock);
		}

		public static bool WasReleased(string id, InputComponent data, bool ignoreBlock = false) {
			return Check(id, CheckType.RELEASED, data, ignoreBlock);
		}

		public static bool IsDown(string id, InputComponent data, bool ignoreBlock = false) {
			return Check(id, CheckType.DOWN, data, ignoreBlock);
		}
	}
}