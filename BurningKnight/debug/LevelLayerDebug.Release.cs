namespace BurningKnight.debug {
	// The layer flags the runtime reads, as plain flags. A release build compiles out the dev
	// window but keeps the checks, so the flags simply stay false (ADR-0003).
	public static class LevelLayerDebug {
		public static bool Blood;
		public static bool Chasms;
		public static bool Floor;
		public static bool Lights;
		public static bool Liquids;
		public static bool Mess;
		public static bool Render;
		public static bool Rocks;
		public static bool Shadows;
		public static bool Sides;
		public static bool TileLight;
		public static bool Walls;
	}
}
