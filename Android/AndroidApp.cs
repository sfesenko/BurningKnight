using BurningKnight;
using AndroidPort.core;

namespace AndroidPort;

// The Android half of the composition root. The surface is always the screen, so the design
// resolution is only a seed: the engine scales 320x180 up to whatever the display reports.
public class AndroidApp() : BK(BurningKnight.Display.Width * Scale, BurningKnight.Display.Height * Scale, true, () => new AndroidCore()) {
	private const int Scale = 3;
}
