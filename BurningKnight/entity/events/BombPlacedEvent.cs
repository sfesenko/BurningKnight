using BurningKnight.entity.bomb;
using Lens.entity;

namespace BurningKnight.entity.events {
	public class BombPlacedEvent : Event {
		public Bomb Bomb = null!;
		public Entity Owner = null!;
	}
}