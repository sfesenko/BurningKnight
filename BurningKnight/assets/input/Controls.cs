using System;
using System.Collections.Generic;
using System.IO;
using BurningKnight.save;
using Lens.input;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.file;
using Microsoft.Xna.Framework.Input;

namespace BurningKnight.assets.input {
	public static class Controls {
		private static List<Control> controls = new List<Control>();
		private static List<Control> custom = new List<Control>();

		public static FileHandle BindingsHandle => new FileHandle($"{SaveManager.SaveDir}keybindings_{Version}.json");
		
		public const int Version = 2;

		public const string Up = "up";
		public const string Left = "left";
		public const string Down = "down";
		public const string Right = "right";
		
		public const string Active = "active";
		public const string Use = "use";
		public const string Bomb = "bomb";
		public const string Interact = "interact";
		public const string Swap = "swap";

		public const string Roll = "roll";
		public const string Duck = "duck";
		
		public const string Pause = "pause";

		public const string GameStart = "game_start";
		public const string Cancel = "cancel";
		
		public const string Fullscreen = "fullscreen";
		public const string Fps = "fps";

		public const string UiUp = "ui_up";
		public const string UiDown = "ui_down";
		public const string UiLeft = "ui_left";
		public const string UiRight = "ui_right";
		public const string UiAccept = "ui_accept";
		public const string UiSelect = "ui_select";
		public const string UiBack = "ui_back";

		public const string QuickRestart = "quick_restart";

		static Controls() {
			controls.Clear();
			
			controls.Add(new Control(Up, Keys.W, Keys.Up));
			controls.Add(new Control(Left, Keys.A, Keys.Left));
			controls.Add(new Control(Down, Keys.S, Keys.Down));
			controls.Add(new Control(Right, Keys.D, Keys.Right));

			controls.Add(new Control(Active, Keys.Space).Gamepad(Buttons.RightShoulder));
			controls.Add(new Control(Use).Mouse(MouseButtons.Left).Gamepad(Buttons.RightTrigger));

			controls.Add(new Control(Bomb, Keys.Q).Gamepad(Buttons.B));
			controls.Add(new Control(Interact, Keys.E).Gamepad(Buttons.X));
			controls.Add(new Control(Swap, Keys.LeftShift).Gamepad(Buttons.A));
			controls.Add(new Control(Roll).Mouse(MouseButtons.Right).Gamepad(Buttons.Y, Buttons.LeftTrigger));
			controls.Add(new Control(Duck, Keys.R).Gamepad(Buttons.LeftShoulder));

			controls.Add(new Control(Pause, Keys.Escape).Gamepad(Buttons.Back));
			
			controls.Add(new Control(Fullscreen, Keys.F11));
			controls.Add(new Control(Fps, Keys.F2));

			controls.Add(new Control(Cancel, Keys.Escape).Gamepad(Buttons.Back));
			controls.Add(new Control(GameStart, Keys.Space, Keys.Enter, Keys.X).Gamepad(Buttons.X, Buttons.Start));
			
			controls.Add(new Control(UiUp, Keys.W, Keys.Up).Gamepad(Buttons.LeftThumbstickUp, Buttons.RightThumbstickUp, Buttons.DPadUp));
			controls.Add(new Control(UiDown, Keys.S, Keys.Down).Gamepad(Buttons.LeftThumbstickDown, Buttons.RightThumbstickDown, Buttons.DPadDown));
			controls.Add(new Control(UiLeft, Keys.A, Keys.Left).Gamepad(Buttons.LeftThumbstickLeft, Buttons.RightThumbstickLeft, Buttons.DPadLeft));
			controls.Add(new Control(UiRight, Keys.D, Keys.Right).Gamepad(Buttons.LeftThumbstickRight, Buttons.RightThumbstickRight, Buttons.DPadRight));
			controls.Add(new Control(UiAccept).Mouse(MouseButtons.Left, MouseButtons.Right));
			controls.Add(new Control(UiSelect, Keys.X, Keys.Enter, Keys.Space).Gamepad(Buttons.X, Buttons.A, Buttons.Y));
			controls.Add(new Control(UiBack, Keys.Escape).Gamepad(Buttons.Back, Buttons.B));
			
			controls.Add(new Control(QuickRestart, Keys.R, Keys.P).Gamepad(Buttons.X));
		}

		public static void Bind() {
			Bind(custom);
		}
		
		public static void BindDefault() {
			Bind(controls);
		}

		private static void Bind(List<Control> controls) {
			Input.ClearBindings();

			foreach (var c in controls) {
				BindChannel(c.Id, c.Keys);
				BindChannel(c.Id, c.Buttons);
				BindChannel(c.Id, c.MouseButtons);
			}
		}

		private static void BindChannel(string id, Keys[]? values) {
			if (values != null) {
				Input.Bind(id, values);
			}
		}

		private static void BindChannel(string id, Buttons[]? values) {
			if (values != null) {
				Input.Bind(id, values);
			}
		}

		private static void BindChannel(string id, MouseButtons[]? values) {
			if (values != null) {
				Input.Bind(id, values);
			}
		}

		public static void Save() {
			try {
				var p = BindingsHandle.FullPath;
				Log.Info($"Saving keybindings to {p}");

				var file = File.CreateText(p);
				
				var root = new JsonObject();

				foreach (var t in (custom.Count == 0 ? controls : custom)) {
					var o = new JsonObject();

					WriteChannel(o, "keys", t.Keys);
					WriteChannel(o, "mouse", t.MouseButtons);
					WriteChannel(o, "gamepad", t.Buttons);
					
					root[t.Id] = o;
				}

				root.Write(file, true);
				file.Close();
			} catch (Exception e) {
				Log.Error(e);
			}
		}

		private static void WriteChannel<T>(JsonObject o, string name, T[]? values) {
			if (values == null) {
				return;
			}

			var a = new JsonArray();

			foreach (var v in values) {
				a.Add(v!.ToString());
			}

			o[name] = a;
		}

		private static T[]? ParseChannel<T>(JsonNode? node) where T : struct, Enum {
			if (node is not JsonArray array) {
				return null;
			}

			var list = new List<T>();

			foreach (var k in array) {
				if (Enum.TryParse<T>(k.String(""), out var value)) {
					list.Add(value);
				} else {
					Log.Error($"Unknown {typeof(T).Name} value {k}");
				}
			}

			// An explicitly empty array means deliberately unbound; a non-empty array
			// with nothing parseable is corrupt, treated as missing (keeps defaults).
			if (list.Count == 0 && array.Count > 0) {
				return null;
			}

			return list.ToArray();
		}

		public static void Load() {
			var handle = BindingsHandle;
			var legacy = false;

			if (!handle.Exists()) {
				// The version rides in the filename above; a v1 file carries forward.
				var old = new FileHandle($"{SaveManager.SaveDir}keybindings_{Version - 1}.json");

				if (!old.Exists()) {
					Log.Info("Keybindings file was not found, creating new one");

					BindDefault();
					Save();

					return;
				}

				handle = old;
				legacy = true;
			}

			try {
				Log.Info("Loading keybindings");

				var root = JsonNode.Parse(handle.ReadAll())!.AsJsonObject()!;
				var parsed = new Dictionary<string, Control>();

				foreach (var pair in root) {
					parsed[pair.Key] = new Control(pair.Key) {
						Keys = ParseChannel<Keys>(pair.Value?["keys"]),
						MouseButtons = ParseChannel<MouseButtons>(pair.Value?["mouse"]),
						Buttons = ParseChannel<Buttons>(pair.Value?["gamepad"])
					};
				}

				// Validate-then-merge: start from the defaults and overlay, per channel,
				// what parsed. Unknown ids and missing channels keep defaults; an
				// explicitly empty array means deliberately unbound (it round-trips
				// through Save, which writes empty arrays as-is).
				var merged = new List<Control>();

				foreach (var def in controls) {
					var over = parsed.TryGetValue(def.Id, out var p) ? p : null;

					merged.Add(new Control(def.Id) {
						Keys = over?.Keys ?? def.Keys,
						MouseButtons = over?.MouseButtons ?? def.MouseButtons,
						Buttons = over?.Buttons ?? def.Buttons
					});
				}

				custom.Clear();
				custom.AddRange(merged);

				Bind();

				if (legacy) {
					Save();
				}
			} catch (Exception e) {
				Log.Error(e);
				BindDefault();
				Quarantine(handle);
			}
		}

		private static void Quarantine(FileHandle handle) {
			try {
				var path = handle.FullPath;
				File.Move(path, $"{path}.corrupt", true);
			} catch (Exception e) {
				Log.Error(e);
			}
		}

		private static string? FindRaw(string id, bool gamepad) {
			foreach (var c in (custom.Count == 0 ? controls : custom)) {
				if (c.Id == id) {
					if (gamepad) {
						if (c.Buttons != null && c.Buttons.Length > 0) {
							return c.Buttons[0].ToString();
						}
					} else if (c.Keys != null && c.Keys.Length > 0) {
						return c.Keys[0].ToString();
					} else if (c.MouseButtons != null && c.MouseButtons.Length > 0) {
						return c.MouseButtons[0].ToString();
					}
				}
			}

			return null;
		}

		public static string FindKeyboard(string id) {
			return Prettify(FindRaw(id, false) ?? "None");
		}

		public static string FindGamepad(string id) {
			return Prettify(FindRaw(id, true) ?? "None");
		}

		private static readonly Dictionary<string, string> PrettyExact = new() {
			["Left"] = "LMB",
			["Right"] = "RMB",
			["Middle"] = "MMB"
		};

		private static string Prettify(string k) {
			if (PrettyExact.TryGetValue(k, out var pretty)) {
				return pretty;
			}

			if (k.Length == 2 && k[0] == 'D') {
				return k[1].ToString();
			}

			foreach (var affix in new[] { "Left", "Right" }) {
				if (k.StartsWith(affix)) {
					return $"{affix} {k[affix.Length..]}";
				}
			}

			foreach (var affix in new[] { "Left", "Right", "Down", "Up" }) {
				if (k.EndsWith(affix)) {
					return $"{k[..^affix.Length]} {affix}";
				}
			}

			return k;
		}


		private static readonly Dictionary<string, string> GamepadSlices = new() {
			["Left Trigger"] = "button_lt",
			["Left Shoulder"] = "button_lb",
			["Right Trigger"] = "button_rt",
			["Right Shoulder"] = "button_rb"
		};

		private static readonly Dictionary<string, string> KeySlices = new() {
			["lmb"] = "button_lmb",
			["rmb"] = "button_rmb"
		};

		private static readonly (string Part, string Slice)[] KeySliceHints = [
			("shift", "key_shift"),
			("caps", "key_capslock"),
			// Matches "control": the legacy substring, kept as-is.
			("ntrl", "key_control")
		];

		public static string? FindSlice(string name, bool gamepad) {
			var id = gamepad ? FindGamepad(name) : FindKeyboard(name);

			if (id == null) {
				return null;
			}

			if (gamepad) {
				if (GamepadSlices.TryGetValue(id, out var slice)) {
					return slice;
				}

				return $"button_{id.ToLower()}";
			}

			id = id.ToLower();

			if (KeySlices.TryGetValue(id, out var key)) {
				return key;
			}

			foreach (var (part, slice) in KeySliceHints) {
				if (id.Contains(part)) {
					return slice;
				}
			}

			return $"key_{id}";
		}

		private static void ReplaceCore(string id, Action<Control> apply) {
			foreach (var c in (custom.Count == 0 ? controls : custom)) {
				if (c.Id == id) {
					apply(c);
					break;
				}
			}
		}

		public static void Replace(string id, Keys key) {
			ReplaceCore(id, c => {
				c.Keys = new[] {key};
				c.MouseButtons = null;
			});
		}

		public static void Replace(string id, Buttons button) {
			ReplaceCore(id, c => {
				c.Buttons = new[] {button};
			});
		}

		public static void Replace(string id, MouseButtons button) {
			ReplaceCore(id, c => {
				c.MouseButtons = new[] {button};
				c.Keys = null;
			});
		}
	}
}