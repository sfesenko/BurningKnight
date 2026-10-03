using Lens.core;
using Microsoft.Xna.Framework.Graphics;

namespace AndroidPort.core;

// Android presents one full-screen surface; there is no windowed mode to toggle. The one thing
// the host must supply is the display's own size, because the engine derives its first view from
// the preferred back buffer before the surface has reported anything.
public class AndroidCore : Core {
	public override bool CanToggleFullscreen => false;

	// A handheld has no mouse-leave signal, so backgrounding always pauses the run,
	// independent of the desktop-oriented Autopause setting.
	public override bool PauseOnBackground => true;

	public override void Init(int width, int height, bool fullscreen) {
		base.Init(width, height, fullscreen);
		RefreshDisplaySize();
	}

	public override void OnDisplayChanged() {
		// CONTRACT: the host routes this onto the game thread, so touching the graphics
		// manager here is safe.
		RefreshDisplaySize();
		Graphics.ApplyChanges();
	}

	private void RefreshDisplaySize() {
		var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

		Graphics.PreferredBackBufferWidth = display.Width;
		Graphics.PreferredBackBufferHeight = display.Height;
	}
}
