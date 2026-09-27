namespace BurningKnight;

// The game's design resolution. The engine has its own copy as state, supplied at startup, so
// it never carries these numbers itself.
public class Display {
	public const int Width = 320;
	public const int Height = 180;
	public const float UiScale = 1.5f;
	public const float Viewport = (float) Width / Height;
	public const int UiWidth = (int) (Width * UiScale);
	public const int UiHeight = (int) (Height * UiScale);
}
