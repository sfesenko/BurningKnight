namespace Lens;

public class Display {
	public static int Width { get; private set; }
	public static int Height { get; private set; }
	public static float Viewport { get; private set; }
	public static float UiScale { get; private set; }
	public static int UiWidth { get; private set; }
	public static int UiHeight { get; private set; }

	// The app supplies the design resolution; the engine must not carry the game's numbers.
	public static void Setup(int width, int height, float uiScale) {
		Width = width;
		Height = height;
		UiScale = uiScale;
		Viewport = (float) width / height;
		UiWidth = (int) (width * uiScale);
		UiHeight = (int) (height * uiScale);
	}
}
