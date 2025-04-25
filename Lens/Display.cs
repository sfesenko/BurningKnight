namespace Lens;

public class Display {
	public static readonly int Width = 320;
	public static readonly int Height = 180;
	public static readonly float Viewport = (float) Width / Height;
	public const float UiScale = 1.5f;
	public static readonly int UiWidth = (int) (Width * UiScale);
	public static readonly int UiHeight = (int) (Height * UiScale);
}