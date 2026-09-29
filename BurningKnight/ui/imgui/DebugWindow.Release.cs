namespace BurningKnight.ui.imgui {
	// The debug flags the runtime reads, as plain flags. A release build compiles out the dev
	// window but keeps the checks, so the flags simply stay false (ADR-0003).
	public static class DebugWindow {
		public static bool ItemShader;
	}
}
