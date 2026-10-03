using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace AndroidPort;

// The whole host bootstrap sits in OnCreate, where Program.Main sits on desktop: writable paths,
// the content archive, then the game. The activity is locked to landscape and handles its own
// configuration changes so a rotation or a keyboard flip never recreates it.
[Activity(
	Label = "Burning Knight",
	MainLauncher = true,
	Exported = true,
	Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
	ScreenOrientation = ScreenOrientation.Landscape,
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.ScreenLayout | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : AndroidGameActivity {
	protected override void OnCreate(Bundle? savedInstanceState) {
		base.OnCreate(savedInstanceState);

		Bootstrap.Setup(this);

		var game = new AndroidApp();
		var view = (View)game.Services.GetService(typeof(View))!;

		SetContentView(view);

		// The game view must own focus or Android hands key and gamepad events to the activity,
		// where nothing forwards them to MonoGame's input.
		view.Focusable = true;
		view.FocusableInTouchMode = true;
		view.RequestFocus();

		game.Run();
	}

	// A DPI/resolution switch or rotation reaches here instead of recreating the
	// activity (see ConfigChanges above), so the run survives it. Only the host's
	// size snapshot is refreshed; MonoGame recreates the surface itself and the
	// engine picks the numbers up through its normal UpdateView path.
	// The back button needs no override: MonoGame's view consumes Keycode.Back and
	// reports it as Buttons.Back, so the activity never finishes from it.
	public override void OnConfigurationChanged(Android.Content.Res.Configuration? newConfig) {
		base.OnConfigurationChanged(newConfig);
		Lens.Engine.Instance.DisplayChanged();
	}
}
