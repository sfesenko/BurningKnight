using Lens.util.camera;

namespace Lens.graphics.gamerenderer {
	public class DefaultGameRenderer : GameRenderer {
		public override void Render() {
			var camera = Engine.Instance.State?.Camera;

			Graphics.Batch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, DefaultRasterizerState, GameEffect, 
				camera == null ? Engine.ScreenMatrix : camera.Matrix * Engine.ScreenMatrix);
			
			Engine.Instance.State?.Render();
			
			Graphics.Batch.End();
		}
	}
}