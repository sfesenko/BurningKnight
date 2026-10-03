using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Lens.util;
using Microsoft.Xna.Framework;

namespace AndroidPort;

// The whole host bootstrap sits in OnCreate, where Program.Main sits on desktop: writable paths,
// the content archive, then the game. The activity is locked to landscape and handles its own
// configuration changes so a rotation, a font-scale switch, or a keyboard flip never recreates it.
// SingleTask so a second launch reuses this activity instead of stacking another game on top.
[Activity(
	Label = "Burning Knight",
	MainLauncher = true,
	Exported = true,
	LaunchMode = LaunchMode.SingleTask,
	Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
	ScreenOrientation = ScreenOrientation.Landscape,
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.ScreenLayout | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.FontScale | ConfigChanges.UiMode)]
public class MainActivity : AndroidGameActivity {
	private AndroidApp? game;
	private bool started;

	protected override void OnCreate(Bundle? savedInstanceState) {
		base.OnCreate(savedInstanceState);

		// MonoGame's activity does not hold the screen: without this the device
		// sleeps mid-run on its normal timeout. Window-scoped, dies with us.
		Window!.AddFlags(WindowManagerFlags.KeepScreenOn);
		ApplyImmersive();

		Android.Util.Log.Info("BK", "OnCreate");

		// Synchronous, like Program.Main on desktop: the unpack runs here on the UI thread.
		// An async attempt (Task.Run + RunOnUiThread) left a black screen on cold start —
		// the game never reached its first frame — so this stays on the proven path.
		// The copy itself is staged and atomic, and the marker skips it on later launches.
		try {
			Bootstrap.Setup(this);
		} catch (Exception e) {
			Android.Util.Log.Info("BK", $"Setup failed: {e.Message}");
			ShowFailure($"Failed to start:\n{e.Message}");

			return;
		}

		StartGame();
	}

	private void ShowFailure(string text) {
		try {
			SetContentView(new TextView(this) {
				Text = text,
				Gravity = GravityFlags.Center
			});
		} catch {
			// Nothing left to show it on.
		}
	}

	private void StartGame() {
		if (started || IsFinishing || IsDestroyed) {
			return;
		}

		Android.Util.Log.Info("BK", "StartGame");

		try {
			var app = new AndroidApp();
			Android.Util.Log.Info("BK", "app created");
			var view = (View) app.Services.GetService(typeof(View))!;

			SetContentView(view);

			// The game view must own focus or Android hands key and gamepad events to the activity,
			// where nothing forwards them to MonoGame's input.
			view.Focusable = true;
			view.FocusableInTouchMode = true;
			view.RequestFocus();

			game = app;
			started = true;

			ApplyImmersive();
			Android.Util.Log.Info("BK", "Run");
			app.Run();
		} catch (Exception e) {
			Android.Util.Log.Info("BK", $"StartGame failed: {e.Message}");
			Log.Error(e);
			ShowFailure($"Failed to start:\n{e.Message}");
		}
	}

	protected override void OnPause() {
		base.OnPause();

		// Saves write through on every save and SaveManager exposes no flush-all API, so the log
		// is the only open handle worth flushing (AutoFlush already covers a kill, this covers
		// the reinstall force-stop). The writer stays open: closing it here would silently end
		// file logging for the rest of the process.
		try {
			Log.Flush();
		} catch {
			// Nowhere left to report to.
		}
	}

	protected override void OnDestroy() {
		try {
			game?.Dispose();
		} catch {
			// Shutting down anyway.
		} finally {
			game = null;
		}

		base.OnDestroy();
	}

	public override void OnWindowFocusChanged(bool hasFocus) {
		base.OnWindowFocusChanged(hasFocus);

		// Dialogs, volume panels, and the like bring the system bars back; take them away again.
		if (hasFocus) {
			ApplyImmersive();
		}
	}

	private void ApplyImmersive() {
		try {
			var window = Window;

			if (window == null) {
				return;
			}

			window.AddFlags(WindowManagerFlags.KeepScreenOn);

			if (OperatingSystem.IsAndroidVersionAtLeast(30)) {
				var controller = window.InsetsController;

				if (controller != null) {
					controller.Hide(Android.Views.WindowInsets.Type.StatusBars() | Android.Views.WindowInsets.Type.NavigationBars());
					controller.SystemBarsBehavior = (int) WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
				}
			} else {
				var decor = window.DecorView;

				if (decor == null) {
					return;
				}

#pragma warning disable CA1422 // Pre-30 fallback; the modern path above runs on 30+.
				decor.SystemUiFlags = SystemUiFlags.HideNavigation | SystemUiFlags.Fullscreen | SystemUiFlags.ImmersiveSticky |
					SystemUiFlags.LayoutHideNavigation | SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutStable;
#pragma warning restore CA1422
			}
		} catch {
			// Cosmetic; the game runs fine without it.
		}
	}

	// A DPI/resolution switch or rotation reaches here instead of recreating the
	// activity (see ConfigChanges above), so the run survives it. Only the host's
	// size snapshot is refreshed; MonoGame recreates the surface itself and the
	// engine picks the numbers up through its normal UpdateView path.
	// The back button needs no override: MonoGame's view consumes Keycode.Back and
	// reports it as Buttons.Back, so the activity never finishes from it.
	public override void OnConfigurationChanged(Android.Content.Res.Configuration? newConfig) {
		base.OnConfigurationChanged(newConfig);

		try {
			// The engine does not exist yet while the unpack label is up.
			Lens.Engine.Instance.DisplayChanged();
		} catch {
			// No game to tell.
		}
	}
}
