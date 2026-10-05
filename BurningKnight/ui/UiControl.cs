using BurningKnight.assets.input;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using Lens.assets;
using Lens.input;
using Lens.util;
using Microsoft.Xna.Framework.Input;
using System.Linq;

namespace BurningKnight.ui {
	public class UiControl : UiButton {
		public static UiControl? Focused = null!;
		public string Key = null!;
		public bool Gamepad;
		// May be null or stale (captured when the pane was built); DoCheck resolves the live pad lazily instead.
		public GamepadComponent? GamepadComponent;

		private float cx;
		private bool firstClickFrame;

		public override void Init() {
			base.Init();

			cx = RelativeX;
			SetLabel();
		}

		public override void Destroy() {
			base.Destroy();

			if (Focused == this) {
				Focused = null;
			}
		}

		public override void OnClick() {
			base.OnClick();

			if (Focused == this) {
				// Focused = null;
				// SetLabel();
			} else {
				firstFrame = true;
				firstClickFrame = true;
				Focused = this;
				Label = $"{Locale.Get(Key)}: {Locale.Get("select")}";
				RelativeCenterX = cx;
			}
		}

		private void SetLabel() {
			var k = Gamepad ? Controls.FindGamepad(Key) : Controls.FindKeyboard(Key);
			
			Label = $"{Locale.Get(Key)}: {k}";
			RelativeCenterX = cx;
		}

		private static Keys[] keysToCheck = {
			Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Y, Keys.U, Keys.I, Keys.O, Keys.P,
			Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, Keys.H, Keys.J, Keys.K, Keys.L,
			Keys.Z, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N, Keys.M, 
			Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9,
			Keys.Up, Keys.Down, Keys.Left, Keys.Right, Keys.Enter, Keys.Escape, Keys.Tab, Keys.CapsLock,
			Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12,
		
			Keys.Space, Keys.LeftShift, Keys.LeftControl, Keys.LeftWindows, Keys.LeftAlt,
			Keys.RightShift, Keys.RightControl, Keys.RightWindows, Keys.RightAlt
		};

		private static MouseButtons[] mouseToCheck = {
			MouseButtons.Left, MouseButtons.Middle, MouseButtons.Right
		};

		// Start and Back stay out: they drive pause and menu flow, so capturing one here would trap the
		// player without a way to pause or back out. BigButton never arrives (consumed by the OS guide).
		private static Buttons[] buttonsToCheck = {
			Buttons.A, Buttons.B, Buttons.X, Buttons.Y, Buttons.LeftShoulder, Buttons.RightShoulder,
			Buttons.LeftStick, Buttons.RightStick, Buttons.LeftTrigger, Buttons.RightTrigger,
			Buttons.DPadDown, Buttons.DPadUp, Buttons.DPadLeft, Buttons.DPadRight
		};

		public void Cancel() {
			Focused = null;
			SetLabel();
		}

		private bool firstFrame;

		public override void Update(float dt) {
			base.Update(dt);

			if (firstFrame) {
				firstFrame = false;
				return;
			}

			DoCheck();
		}

		public void DoCheck() {
			if (firstFrame) {
				return;
			}
			
			if (Focused == this) {
				if (Gamepad) {
					// The stored component may be null or stale (captured when the pane was built),
					// so resolve the live pad at remap time: the local player's, then any attached pad.
					var component = GamepadComponent;

					if (Area != null && component?.Controller == null) {
						component = LocalPlayer.Locate(Area)?.GetComponent<GamepadComponent>();
					}

					var controller = component?.Controller ?? Input.Gamepads.FirstOrDefault(g => g.Attached);

					if (controller == null) {
						Log.Error("Null controller");
						return;
					}

					
					foreach (var b in buttonsToCheck) {
						if (controller.WasPressed(b)) {
							Controls.Replace(Key, b);
							Controls.Bind();
							Controls.Save();
						
							Focused = null;
							SetLabel();
						
							break;
						}
					}
				} else {
					foreach (var k in keysToCheck) {
						if (Input.Keyboard.WasPressed(k)) {
							
							Controls.Replace(Key, k);
							Controls.Bind();
							Controls.Save();
						
							Focused = null;
							SetLabel();
						
							break;
						}
					}

					if (firstClickFrame) {
						firstClickFrame = false;
					} else {
						foreach (var b in mouseToCheck) {
							if (Input.Mouse.Check(b, Input.CheckType.PRESSED)) {

								Controls.Replace(Key, b);
								Controls.Bind();
								Controls.Save();

								Focused = null;
								SetLabel();

								break;
							}
						}
					}
				}
			}
		}
	}
}