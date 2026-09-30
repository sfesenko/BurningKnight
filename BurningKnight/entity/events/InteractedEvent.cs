using Lens.entity;

namespace BurningKnight.entity.events {
	public class InteractedEvent : Event {
		public Entity Who = null!;
		public Entity With = null!;
	}
}