using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lens.graphics.gamerenderer {
	public class GameRenderer {
		public static bool EnableBatcher = true;
		
		public RenderTarget2D GameTarget;
		public RenderTarget2D UiTarget;

		public static SpriteSortMode DefaultSortMode = SpriteSortMode.Deferred;

		// The display scale the game draws at (the "scale" setting; 1 or 2). One display, one
		// scale, whichever renderer is active.
		public static float GameScale;

		// Clipping to a world-space rectangle. The pixel-perfect renderer scissors to it; the
		// others store the values and ignore them.
		public bool EnableClip = false;
		public Vector2 ClipPosition;
		public Vector2 ClipSize;

		public SpriteSortMode SpriteSortMode = DefaultSortMode;
		public BlendState BlendState = BlendState.NonPremultiplied;
		public SamplerState SamplerState = SamplerState.PointClamp;
		public DepthStencilState DepthStencilState = DepthStencilState.None;
		public RasterizerState DefaultRasterizerState = RasterizerState.CullNone;
		public RasterizerState ClipRasterizerState = new RasterizerState {
			ScissorTestEnable = true
		};
		
		public Effect GameEffect;
		public Effect UiEffect;
		public Effect SurfaceEffect;
		public Color Bg = Color.Black;

		public virtual void Begin() {
			
		}

		public virtual void End() {
			
		}

		// Begin the UI pass. The pixel-perfect renderer draws it into its UI target with the UI
		// scale; a renderer without a separate UI pass starts a batch on the current target.
		public virtual void BeginUi(bool force = false) {
			if (!force) {
				Engine.GraphicsDevice.SetRenderTarget(UiTarget);
			}

			Graphics.Batch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, DefaultRasterizerState, SurfaceEffect, Matrix.Identity);
		}

		// The shadow pass draws into a separate target in the pixel-perfect renderer; the others
		// have no shadow target, so the pass is a no-op.
		public virtual void BeginShadows() {
			
		}

		public virtual void EndShadows() {
			
		}
		
		public virtual void Render() {
			
		}

		public virtual void Destroy() {
			UiTarget?.Dispose();
			GameTarget?.Dispose();
		}

		public virtual void Resize(int width, int height) {
			
		}
	}
}