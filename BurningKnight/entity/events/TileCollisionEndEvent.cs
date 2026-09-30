using BurningKnight.level.tile;
using Lens.entity;

namespace BurningKnight.entity.events {
	public class TileCollisionEndEvent : Event {
		public Entity Who = null!;
		public Tile Tile;
	}
}