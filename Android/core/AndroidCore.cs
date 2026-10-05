using System;
using Android.App;
using Lens.core;
using Lens.util;
using Microsoft.Xna.Framework.Graphics;

namespace AndroidPort.core;

// One full-screen surface, no windowed mode. The host must supply the display's own size:
// the engine derives its first view from the preferred backbuffer before the surface reports.
public class AndroidCore(Activity activity) : Core {
	public override bool CanToggleFullscreen => false;

	// A handheld has no mouse-leave signal, so backgrounding always pauses the run,
	// independent of the desktop-oriented Autopause setting.
	public override bool PauseOnBackground => true;

	public override void Quit() {
		// Exit() only MoveTaskToBack here: a relaunch would return to the same dead screen.
		// Finish the activity; OnDestroy disposes the game.
		try {
			activity.FinishAndRemoveTask();
		} catch (Exception e) {
			Log.Error(e);
			base.Quit();
		}
	}

	public override void Init(int width, int height, bool fullscreen) {
		base.Init(width, height, fullscreen);
		RefreshDisplaySize();
	}

	public override void OnDisplayChanged() {
		// CONTRACT: host routes this onto the game thread. Guarded: runs inside the frame-dispatch
		// task — a throw here silently ends the render loop (freeze, no crash UI).
		try {
			RefreshDisplaySize();
			Graphics.ApplyChanges();
		} catch (Exception e) {
			Log.Error(e);
		}
	}

	private void RefreshDisplaySize() {
		var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

		Graphics.PreferredBackBufferWidth = display.Width;
		Graphics.PreferredBackBufferHeight = display.Height;
	}
}
