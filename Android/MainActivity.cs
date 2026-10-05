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
// the content archive, then the game. Landscape (sensor); configChanges keep rotation/font-scale/
// from recreating the activity (locale does — Setup is idempotent); SingleTask reuses it.
[Activity(
	Label = "Burning Knight",
	MainLauncher = true,
	Exported = true,
	LaunchMode = LaunchMode.SingleTask,
	Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
	ScreenOrientation = ScreenOrientation.SensorLandscape,
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.ScreenLayout | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.FontScale | ConfigChanges.UiMode)]
public class MainActivity : AndroidGameActivity {
	private AndroidApp? game;
	private bool started;

	protected override void OnCreate(Bundle? savedInstanceState) {
		base.OnCreate(savedInstanceState);

		// Re-adds KeepScreenOn before the game exists; null-checks the window.
		ApplyImmersive();

		// Synchronous boot on the UI thread, like Program.Main: an async attempt black-screened
		// on cold start. Content is read in place from the APK — no unpack step.
		try {
			// Inside the guard: a log throw must not bypass WriteCrash/ShowFailure.
			Android.Util.Log.Info("BK", "OnCreate");

			Bootstrap.Setup(this);
		} catch (Exception e) {
			Android.Util.Log.Info("BK", $"Setup failed: {e.Message}");
			Log.Error(e);
			Bootstrap.WriteCrash(this, "setup", e);
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

		try {
			Android.Util.Log.Info("BK", "StartGame");

			var app = new AndroidApp(this);

			// Assign before anything else can throw: OnDestroy disposes `game`. Never dispose here —
			// MonoGame keeps its own Game ref until OnDestroy; OnResume would deref a disposed game.
			game = app;

			Android.Util.Log.Info("BK", "app created");
			var view = app.Services.GetService(typeof(View)) as View
				?? throw new InvalidOperationException("MonoGame view service missing: expected an Android View from AndroidGameActivity.");

			SetContentView(view);

			// The game view must own focus or Android hands key and gamepad events to the activity,
			// where nothing forwards them to MonoGame's input.
			view.Focusable = true;
			view.FocusableInTouchMode = true;
			view.RequestFocus();

			// Fires after the layout pass with final size; re-arms the flag onConfigurationChanged
			// consumed too early. Idempotent.
			view.LayoutChange += (_, _) => Lens.Engine.Instance?.DisplayChanged();

			started = true;

			ApplyImmersive();
			Android.Util.Log.Info("BK", "Run");
			app.Run();
		} catch (Exception e) {
			Android.Util.Log.Info("BK", $"StartGame failed: {e.Message}");
			Log.Error(e);
			Bootstrap.WriteCrash(this, "start", e);
			ShowFailure($"Failed to start:\n{e.Message}");
		}
	}

	// Half-built game (StartGame's ctor threw) is subscribed to Resumed: base would NRE
	// past ShowFailure. Nothing to feed — OnDestroy's base disposes it.
	protected override void OnResume() {
		if (game != null) {
			base.OnResume();
		}
	}

	protected override void OnPause() {
		base.OnPause();

		// Flush the log (the only open handle; saves write through). Never close it here —
		// that would silently end file logging for the rest of the process.
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

		try {
			// base re-invokes Game.Dispose(): re-enters a half-disposed game if ours threw above.
			base.OnDestroy();
		} catch (Exception e) {
			Log.Error(e);
		}
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
		} catch (Exception e) {
			// Sleeps on the normal timeout without it; breadcrumb anyway (looks like a hang).
			try {
				Android.Util.Log.Info("BK", $"KeepScreenOn failed: {e.Message}");
			} catch {
				// Cosmetic; the game runs fine without it.
			}
		}

		try {
			var window = Window;

			if (window == null) {
				return;
			}

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

	// Rotation/DPI switch lands here instead of recreating the activity; UpdateView recomputes
	// scale/viewport/targets. Back needs no override: the view reports Keycode.Back as Buttons.Back.
	public override void OnConfigurationChanged(Android.Content.Res.Configuration? newConfig) {
		base.OnConfigurationChanged(newConfig);

		try {
			// The engine does not exist yet before the game is constructed.
			Lens.Engine.Instance?.DisplayChanged();
		} catch {
			// No game to tell.
		}
	}
}
