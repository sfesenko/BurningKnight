namespace BurningKnight.debug {
	// The layer flags the runtime reads. A release build compiles out the dev window but keeps the
	// checks, so the flags hold the same defaults the window starts with (ADR-0003).
	public static class LevelLayerDebug {
		public static bool Chasms = true;
		public static bool Floor = true;
		public static bool Liquids = true;
		public static bool Mess = true;
		public static bool Sides = true;
		public static bool Walls = true;
		public static bool Blood = true;
		public static bool Lights = true;
		public static bool TileLight = true;
		public static bool Shadows = true;
		public static bool Rocks = true;
	}
}
