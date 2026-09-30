using Lens.util.camera;
using Microsoft.Xna.Framework;

namespace Lens.graphics.gamerenderer {
	public class NativeGameRenderer : GameRenderer {
		public override void Render() {
			Begin();
			Engine.Instance.State.Render();
			End();
			
			Graphics.Batch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, DefaultRasterizerState, GameEffect, Matrix.Identity * Matrix.CreateScale(2));
			Engine.Instance.State.RenderUi();
			End();

			Engine.Instance.State.RenderNative();
		}

		public override void Begin() {
			Graphics.Batch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, DefaultRasterizerState, GameEffect, 
				Engine.Instance.State?.Camera?.Matrix ?? Matrix.Identity);
		}

		public override void End() {
			Graphics.Batch.End();
		}
	}
}