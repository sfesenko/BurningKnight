#nullable enable

using System;
using Microsoft.Xna.Framework;

namespace Lens.core;

public class Core {
	protected GameWindow Window = null!; // Core.Create assigns them
	protected GraphicsDeviceManager Graphics = null!;
		
	public virtual void Init(int width, int height, bool fullscreen) {
			
	}

	public virtual void SetWindowed(int width, int height) {
			
	}

	public virtual void SetFullscreen() {
			
	}

	// The host decides which core the engine runs on, so a platform can supply its own without
	// the engine naming it.
	public static Core Create(GameWindow window, GraphicsDeviceManager graphics, Func<Core> factory) {
		if (factory == null) {
			throw new ArgumentNullException(nameof(factory));
		}

		var core = factory();

		core.Window = window;
		core.Graphics = graphics;

		return core;
	}
}