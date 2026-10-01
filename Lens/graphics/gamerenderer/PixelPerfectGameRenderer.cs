using Lens.entity.component.logic;
using Lens.input;
using Lens.util.camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;

namespace Lens.graphics.gamerenderer {
	public class PixelPerfectGameRenderer : GameRenderer {
		public Batcher2D Batcher2D;

		private Matrix one = Matrix.Identity;
		private Matrix uiScale = Matrix.Identity;

		private bool inUi;

		public RasterizerState RasterizerState;

		public RasterizerState GetState => EnableClip ? RasterizerState : DefaultRasterizerState;

		public PixelPerfectGameRenderer() {
			GameTarget = new RenderTarget2D(
				Engine.GraphicsDevice, Display.Width + 1, Display.Height + 1, false,
				Engine.Graphics.PreferredBackBufferFormat, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents
			);
			
			Batcher2D = new Batcher2D(Engine.GraphicsDevice);
			RasterizerState = new RasterizerState();
			RasterizerState.ScissorTestEnable = true;
		}

		public override void Begin() {
			if (inUi) {
				BeginUi();
				return;
			}
			
			Graphics.Batch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, DefaultRasterizerState, SurfaceEffect, Engine.Instance.State?.Camera?.Matrix ?? one);
		}

		public override void End() {
			Graphics.Batch.End();
		}
		
		public override void BeginShadows() {
			Engine.GraphicsDevice.SetRenderTarget(UiTarget);
			Graphics.Batch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, EnableClip ? ClipRasterizerState : DefaultRasterizerState, SurfaceEffect, Engine.Instance.State?.Camera?.Matrix ?? one);
			Graphics.Clear(Color.Transparent);
		}

		public override void EndShadows() {
			Graphics.Batch.End();
			Engine.GraphicsDevice.SetRenderTarget(GameTarget);
		}

		private void RenderGame() {
			Engine.GraphicsDevice.SetRenderTarget(GameTarget);
			Begin();
			Graphics.Clear(Bg);
			Engine.Instance.State?.Render();
			End();
		}

		public override void BeginUi(bool force = false) {
			if (!force) {
				Engine.GraphicsDevice.SetRenderTarget(UiTarget);
			}

			var defaultRasterizerState = EnableClip ? ClipRasterizerState : DefaultRasterizerState;
			var transformMatrix = force ? Matrix.Identity : uiScale;
			Graphics.Batch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, defaultRasterizerState, SurfaceEffect, transformMatrix);
		}

		private void RenderUi() {
			if (UiTarget == null) {
				return;
			}

			BeginUi();
			
			if (Engine.Flashes && Engine.Instance.Flash > 0) {
				Graphics.Clear(Engine.Instance.FlashColor);
			} else {
				Graphics.Clear(Color.Transparent);
			}
			
			Engine.Instance.State?.RenderUi();
			
			End();
		}
		
		public override void Render() {
			var start = EnableBatcher;
			
			if (start) {
				Batcher2D.Begin();
			}
			
			RenderGame();
			inUi = true;
			RenderUi();
			inUi = false;

			Engine.GraphicsDevice.SetRenderTarget(null);
			var set = false;
			var camera = Engine.Instance.State?.Camera;

			if (EnableClip && camera != null) {
				var pos = camera.CameraToScreen(ClipPosition) - new Vector2(camera.Position.X % 1, camera.Position.Y % 1) + camera.GetComponent<ShakeComponent>()!.Position;
				
				Engine.GraphicsDevice.ScissorRectangle = new Rectangle((int) (pos.X * Engine.Instance.Upscale), (int) (pos.Y * Engine.Instance.Upscale), (int) (ClipSize.X * Engine.Instance.Upscale), (int) (ClipSize.Y * Engine.Instance.Upscale));
			} else {
				set = true;
				Engine.GraphicsDevice.ScissorRectangle = new Rectangle((int) Engine.Viewport.X, (int) Engine.Viewport.Y,
					(int) (Display.Width * Engine.Instance.Upscale), (int) (Display.Height * Engine.Instance.Upscale));
			}

			Graphics.Batch.Begin(SpriteSortMode.Immediate, BlendState, SamplerState, DepthStencilState, ClipRasterizerState, GameEffect, one);

			if (camera != null) {
				var shake = camera.GetComponent<ShakeComponent>()!;
				var scale = Engine.Instance.Upscale * camera.TextureZoom;

				Graphics.Render(GameTarget!,
					new Vector2(Engine.Viewport.X + Display.Width / 2f * Engine.Instance.Upscale + scale * shake.Position.X,
						Engine.Viewport.Y + Display.Height / 2f * Engine.Instance.Upscale + scale * shake.Position.Y),
					shake.Angle,
					new Vector2(camera.Position.X % 1 + Display.Width / 2f,
						camera.Position.Y % 1 + Display.Height / 2f),
					new Vector2(scale * GameScale));
			}

			Graphics.Batch.End();

			if (!set) {
				Engine.GraphicsDevice.ScissorRectangle = new Rectangle((int) Engine.Viewport.X, (int) Engine.Viewport.Y,
					(int) (Display.Width * Engine.Instance.Upscale), (int) (Display.Height * Engine.Instance.Upscale));
			}
			
			if (UiTarget != null) {
				Graphics.Batch.Begin(SpriteSortMode.Immediate, BlendState, SamplerState, DepthStencilState, ClipRasterizerState, UiEffect, one);

				if (UiEffect != null) {
					UiEffect.Parameters["bottom"].SetValue(1f);
					Graphics.Render(UiTarget, Engine.Viewport + new Vector2(0, Engine.Instance.UiUpscale));
					UiEffect.Parameters["bottom"].SetValue(0f);
				}
				
				Graphics.Render(UiTarget, Engine.Viewport);
				Graphics.Batch.End();
			}
			

			Engine.Instance.State?.RenderNative();

			if (start) {
				Batcher2D.End();
			}
		}

		public override void Resize(int width, int height) {
			base.Resize(width, height);

			UiTarget?.Dispose();

			UiTarget = new RenderTarget2D(
				Engine.GraphicsDevice, (int) (Display.UiWidth * Engine.Instance.Upscale),
				(int) (Display.UiHeight * Engine.Instance.Upscale), false,
				Engine.Graphics.PreferredBackBufferFormat, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents
			);
			
			uiScale = Matrix.Identity * Matrix.CreateScale(Engine.Instance.UiUpscale);
		}

		public override void Destroy() {
			base.Destroy();
			
			GameTarget?.Dispose();
			UiTarget?.Dispose();
			Batcher2D.Dispose();
		}
	}
}
