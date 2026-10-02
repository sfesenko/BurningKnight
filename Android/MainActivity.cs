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
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
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
}
