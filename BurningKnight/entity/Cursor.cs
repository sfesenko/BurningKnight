using System;
using System.Linq;
using BurningKnight.assets;
using BurningKnight.assets.input;
using BurningKnight.entity.creature.player;
using BurningKnight.state;
using Lens;
using Lens.entity;
using Lens.graphics;
using Lens.input;
using Lens.util;
using Lens.util.camera;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;

namespace BurningKnight.entity {
	public class Cursor : Entity, CustomCameraJumper {
		private static TextureRegion[] regions = null!;

		private Vector2 scale = new Vector2(1);
		private Vector2 stickOffset;
		private bool needsAdjusting = true;
		private bool readTint = true;
		private bool wasStickFiring;
		private Color tint;
		private Vector2 lastPos;

		public Player Player = null!;
		public Vector2 GamePosition;

		// Right-stick deadzone shared with ActiveWeaponComponent: engage past Enter, release below
		// Exit, so edge flicker doesn't stutter aim or trigger.
		public const float StickFireDeadzone = 0.25f;
		public const float StickFireEnter = StickFireDeadzone;
		public const float StickFireExit = 0.2f;

		public static bool StickFiring(GamepadData? data, bool wasFiring = false) {
			if (data == null) {
				return false;
			}

			var threshold = wasFiring ? StickFireExit : StickFireEnter;
			return data.GetRightStick(threshold).LengthSquared() > 0.001f;
		}

		// The weapon updates before this entity (Area before TopUi): a flick must snap the aim
		// or its first shot fires at the old side of the player.
		public void SnapToStick(GamepadData data) {
			var stick = data.GetRightStick();
			var l = stick.Length();

			if (l <= 0.0001f) {
				return;
			}

			stickOffset = stick / l;
			Position = Context.Camera!.CameraToUi(GamePosition = Player.Center + stickOffset * (48 * Settings.CursorRadius));
		}

		public override void Init() {
			base.Init();

			AlwaysActive = true;
			AlwaysVisible = true;
			Depth = Layers.Cursor;
			Width = 0;
			Height = 0;
			
			AddTag(Tags.Cursor);
			regions ??=
			[
				CommonAse.Ui.GetSlice("cursor_a")!,
				CommonAse.Ui.GetSlice("cursor_b")!,
				CommonAse.Ui.GetSlice("cursor_c")!,
				CommonAse.Ui.GetSlice("cursor_d")!,
				CommonAse.Ui.GetSlice("cursor_e")!,
				CommonAse.Ui.GetSlice("cursor_f")!,
				CommonAse.Ui.GetSlice("cursor_g")!,
				CommonAse.Ui.GetSlice("cursor_j")!,
				CommonAse.Ui.GetSlice("cursor_k")!
			];
		}

		public override void Update(float dt) {
			var camera = Context.Camera!;
			base.Update(dt);

			if (Player.Dead) {
				var found = Area!.Entities.Entities.Any(e => e is Cursor && e != this);

				if (found) {
					Done = true;
					return;
				}
			}
			
			if (readTint) {
				readTint = false;
				tint = Player.Tint;
			}

			var input = Player.GetComponent<InputComponent>();
			
			if (input!.KeyboardEnabled && (Input.Mouse.WasMoved || !input.GamepadEnabled || input.GamepadData == null || input.GamepadData.Attached)) {
				var pos = Input.Mouse.ScreenPosition;

				if (pos != lastPos) {
					lastPos = pos;
					Position = camera.CameraToUi(GamePosition = camera.ScreenToCamera(pos));
				}
			}

			var controller = input.GamepadEnabled ? input.GamepadData : null;
			
			if (controller != null && Engine.Instance.State is InGameState { Paused: false, Died: false } && !Context.Run.Won) {
				if (needsAdjusting) {
					needsAdjusting = false;
					Position = camera.CameraToUi(GamePosition = Player.Center);
				}
				
				var stick = controller.GetRightStick();

				var dx = stick.X;
				var dy = stick.Y;
				var d = (float) Math.Sqrt(dx * dx + dy * dy);

				if (d > 1) {
					stick /= d;
				} else {
					stick *= d;
				}

				var l = stick.Length();

				var firing = StickFiring(controller, wasStickFiring);
				wasStickFiring = firing;

				if (firing) {
					var target = MathUtils.CreateVector(Math.Atan2(dy, dx), 1f);

					dx = target.X - stickOffset.X;
					dy = target.Y - stickOffset.Y;

					d = (float) Math.Sqrt(dx * dx + dy * dy);

					if (d > 1) {
						dx /= d;
						dy /= d;
					} else {
						dx *= d;
						dy *= d;
					}

					stickOffset += l * new Vector2(dx, dy) * dt * 10f * Settings.Sensivity;
					Position = camera.CameraToUi(GamePosition = (Player.Center + stickOffset * (48 * Settings.CursorRadius)));

					double a = 0;
					var pressed = false;

					if (controller.DPadLeftCheck) {
						a = Math.PI;
						pressed = true;
					} else if (controller.DPadDownCheck) {
						a = Math.PI / 2f;
						pressed = true;
					} else if (controller.DPadUpCheck) {
						a = Math.PI * 1.5f;
						pressed = true;
					} else if (controller.DPadRightCheck) {
						pressed = true;
					}

					if (pressed) {
						Position = camera.CameraToUi(GamePosition = (Player.Center + MathUtils.CreateVector(a, 48)));
					}
				}
			}

			if (Input.WasPressed(Controls.Use, input)) {
				Tween.To(1.3f, scale.X, x => { scale.X = scale.Y = x; }, 0.05f).OnEnd = () =>
					Tween.To(1f, scale.X, x => { scale.X = scale.Y = x; }, 0.15f);
			}
		}

		public override void Render() {
			if (Settings.HideCursor) {
				return;
			}

			var r = regions[Settings.Cursor];

			if (InGameState.Multiplayer) {
				Graphics.Color = tint;
			}

			Graphics.Render(r, Position, 0, r.Center, scale);
			Graphics.Color = ColorUtils.WhiteColor;
		}

		public Vector2 Jump(Camera.Target target) {
			return new Vector2();
			/*Position = Camera.Instance.CameraToUi(GamePosition = Camera.Instance.ScreenToCamera(Input.Mouse.ScreenPosition));
			return new Vector2((CenterX - Display.UiWidth * 0.5f) * target.Priority, (CenterY - Display.UiHeight * 0.5f) * target.Priority * Display.Viewport * 1.6f)*/;	
		}
	}
}