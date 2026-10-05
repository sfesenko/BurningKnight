using System;
using BurningKnight.assets.items;
using BurningKnight.state;
using Lens;
using Lens.entity.component;
using Lens.entity.component.logic;
using Lens.input;
using Lens.util;
using Lens.util.camera;
using Microsoft.Xna.Framework.Input;

namespace BurningKnight.entity.component {
	public class GamepadComponent : Component {
		public static GamepadData? Current;

		private GamepadData? controller;

		public GamepadData? Controller {
			get => controller;

			set {
				controller = value;
				Entity.GetComponent<InputComponent>()!.GamepadData = value;
			}
		}

		public string? GamepadId = null!;

		static GamepadComponent() {
			Camera.OnShake += amount => {
				if (Current == null || !Settings.Vibrate || Settings.Gamepad == null) {
					return;
				}

				// Level generation closes hidden doors and settles tiles with small shakes; the
				// accumulated camera amount crosses any threshold after a few of them, so the
				// decision is made on the shake that was just requested, not the running total.
				if (amount < 8) {
					return;
				}

				// Only a run in progress has a reason to buzz.
				if (Engine.Instance.State is not InGameState) {
					return;
				}

				// Strength was pinned at 1 by Math.Max(1, ...), so every shake hit full power.
				var a = Math.Clamp(amount / 20f, 0.1f, 1f);
				Current.Rumble(a, Math.Max(0.1f, a * 0.5f));
			};
		}

		public override void Init() {
			base.Init();
			UpdateState();
		}

		public override void Destroy() {
			base.Destroy();
			Controller?.StopRumble();
		}

		private void UpdateState() {
			if (Settings.Gamepad != GamepadId && Settings.Gamepad != null) {
				for (int i = 0; i < 4; i++) {
					var c = GamePad.GetCapabilities(i);
					
					if (c.IsConnected && c.Identifier == Settings.Gamepad) {
						Controller = Input.Gamepads[i];
						GamepadId = Settings.Gamepad;
						Current = Controller;
						
						Log.Info($"Connected {GamePad.GetState(i)}");
						Items.Unlock("bk:gamepad");
						
						break;
					}
				}
				
				Settings.Gamepad = null;
			} else if (Controller == null) {
				for (int i = 0; i < 4; i++) {
					if (GamePad.GetCapabilities(i).IsConnected) {
						Controller = Input.Gamepads[i];
						GamepadId = GamePad.GetCapabilities(i).Identifier;
						Current = Controller;
						
						Settings.Gamepad = GamepadId;
						Log.Info($"Connected {GamePad.GetState(i)}");
						Items.Unlock("bk:gamepad");
						
						return;
					}
				}
				
				Settings.Gamepad = null;
			}
		}

		public override void Update(float dt) {
			base.Update(dt);
			UpdateState();
		}
	}
}