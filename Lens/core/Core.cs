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

	// Whether the platform has a windowed mode to switch to. Android always fills the screen.
	public virtual bool CanToggleFullscreen => true;

	// Whether losing the window should pause the run. Desktop leaves this to the
	// mouse-oriented Autopause setting; a handheld has no mouse-leave signal.
	public virtual bool PauseOnBackground => false;

	// How the run ends on a quit request. The default is MonoGame's Exit(); a host where
	// Exit() only backgrounds the task (Android) overrides to finish for real.
	public virtual void Quit() {
		Engine.Instance.Exit();
	}

	// The display may have changed size (rotation, DPI/resolution switch) without
	// recreating the host. Hosts that snapshot the size re-read it here.
	public virtual void OnDisplayChanged() {

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