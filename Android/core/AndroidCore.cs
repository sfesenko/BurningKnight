using Lens.core;
using Microsoft.Xna.Framework.Graphics;

namespace AndroidPort.core;

// Android presents one full-screen surface; there is no windowed mode to toggle. The one thing
// the host must supply is the display's own size, because the engine derives its first view from
// the preferred back buffer before the surface has reported anything.
public class AndroidCore : Core {
	public override void Init(int width, int height, bool fullscreen) {
		base.Init(width, height, fullscreen);

		var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

		Graphics.PreferredBackBufferWidth = display.Width;
		Graphics.PreferredBackBufferHeight = display.Height;
	}
}
