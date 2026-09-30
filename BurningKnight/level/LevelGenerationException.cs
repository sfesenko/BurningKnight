using System;

namespace BurningKnight.level {
	// A generation attempt that cannot produce a playable level — the painter gave up, or two
	// rooms could not be connected. `GenerationThread` catches exactly this, logs it and retries
	// with a new seed; every other exception is a bug and is left to propagate.
	public class LevelGenerationException : Exception {
		public LevelGenerationException(string message) : base(message) {
		}
	}
}
