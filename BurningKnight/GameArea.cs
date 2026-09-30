#nullable enable

using Lens.entity;

namespace BurningKnight {
	// An Area that carries the game context, so anything holding a world can reach the game's
	// state without a global: `entity.Context`.
	public class GameArea : Area {
		public GameContext Context;

		public GameArea() {
			Context = GameContext.Current;
		}

		public GameArea(GameContext context) {
			Context = context;
		}
	}
}
