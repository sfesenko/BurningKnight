using System;
using BurningKnight.assets.input;
using BurningKnight.entity.component;
using Lens.assets;
using Lens.input;
using Lens.util;

namespace BurningKnight.ui {
	public class UiChoice : UiButton {
		private float cx;
		private int option;

		public bool Disabled;
		
		public int Option {
			get => option;

			set {
				option = value;

				if (Options != null && Options.Length > 0) {
					Label = $"{Locale.Get(Name)}: {Locale.Get(Options[Option])}";
					RelativeCenterX = cx;
				}
			}
		}
		
		public string Name = "";
		public string[]? Options;

		public override void Init() {
			base.Init();

			cx = RelativeX;
			Option = option;
		}

		public override void OnClick() {
			if (!Disabled) {
				Change(Input.Mouse.CheckRightButton ? -1 : 1);
			}
		}

		private void Change(int direction) {
			var o = Option + direction;

			if (o < 0) {
				Option = Options!.Length - 1;
			} else if (o >= Options!.Length) {
				Option = 0;
			} else {
				Option = o;
			}

			base.OnClick();
		}

		public Action<UiChoice>? OnUpdate;

		public override void Update(float dt) {
			OnUpdate?.Invoke(this);

			// Like the sliders: left and right change the value while the choice is selected.
			if (Selected == Id && !Disabled) {
				if (Input.WasPressed(Controls.UiLeft, GamepadComponent.Current, true)) {
					Change(-1);
				} else if (Input.WasPressed(Controls.UiRight, GamepadComponent.Current, true)) {
					Change(1);
				}
			}

			base.Update(dt);
		}
	}
}