using System;
using Lens;
using Lens.assets;
using Lens.graphics.gamerenderer;
using Microsoft.Xna.Framework.Graphics;

namespace BurningKnight.assets {
	public class Shaders {
		public static Effect Ui = null!;
		public static Effect Entity = null!;
		public static Effect Terrain = null!;
		public static Effect Screen = null!;
		public static Effect Fog = null!;
		public static Effect Chasm = null!;
		public static Effect Item = null!;
		public static Effect Bk = null!;
		
		public static void Load() {
			Ui = Effects.Require("ui");
			Entity = Effects.Require("entity");
			Terrain = Effects.Require("terrain");
			Screen = Effects.Require("screen");
			Fog = Effects.Require("fog");
			Chasm = Effects.Require("chasm");
			Item = Effects.Require("item");
			Bk = Effects.Require("bk");

			Engine.Instance.StateRenderer.GameEffect = Screen;
			// Engine.Instance.StateRenderer.UiEffect = Ui;
		}

		public static void Begin(Effect effect) {
			var state = Engine.Instance.StateRenderer;
			
			state.End();
			state.SurfaceEffect = effect;
			state.SpriteSortMode = SpriteSortMode.Immediate;
			effect.CurrentTechnique.Passes[0].Apply();
			state.Begin();
		}

		public static void End() {
			var state = Engine.Instance.StateRenderer;
			
			state.End();
			state.SpriteSortMode = GameRenderer.DefaultSortMode;
			state.SurfaceEffect = null;
			state.Begin();
		}
	}
}