using BurningKnight.level.tile;
using BurningKnight.state;

namespace BurningKnight.entity.room.controllable {
	public class Piston {
		public readonly int X;
		public readonly int Y;

		public Piston(int x, int y) {
			X = x;
			Y = y;
		}

		public bool IsOn() {
			return Context.Level!.Get(X, Y) == Tile.Piston;
		}

		public void Set(bool value) {
			var level = Context.Level!;
			if (IsOn() != value) {
				level.Set(X, Y, value ? Tile.Piston : Tile.PistonDown);
				level.ReCreateBodyChunk(X, Y);
				level.UpdateTile(X, Y);
			}
		}

		public void Toggle() {
			Set(!IsOn());
		}
	}
}