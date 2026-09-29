namespace BurningKnight.ui.imgui {
	// The debug flags the runtime reads. A release build compiles out the dev window but keeps the
	// checks, so the flags hold the same defaults the window starts with (ADR-0003).
	public static class DebugWindow {
		public static bool ItemShader = true;
	}
}
