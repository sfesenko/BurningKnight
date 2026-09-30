using Lens.entity;

namespace BurningKnight.entity.events {
	public class MaxHealthModifiedEvent : Event {
		public Entity Who = null!;
		public int Amount;
	}
}