namespace BurningKnight.debug {
	// The cheat toggles the runtime reads, as plain flags. A release build compiles out the dev
	// window but keeps the checks, so the flags simply stay false (ADR-0003).
	public static class CheatWindow {
		public static bool AutoGodMode;
		public static bool NoSleep;
		public static bool InfiniteActive;
	}
}
