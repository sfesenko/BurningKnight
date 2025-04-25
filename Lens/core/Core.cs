using Microsoft.Xna.Framework;

namespace Lens.Core;

public class Core {
	protected GameWindow Window;
	protected GraphicsDeviceManager Graphics;
		
	public virtual void Init(int width, int height, bool fullscreen) {
			
	}

	public virtual void SetWindowed(int width, int height) {
			
	}

	public virtual void SetFullscreen() {
			
	}

	public static Core SelectCore(GameWindow Window, GraphicsDeviceManager Graphics) {
		Core core = new DesktopCore();

		core.Window = Window;
		core.Graphics = Graphics;

		return core;
	}
}