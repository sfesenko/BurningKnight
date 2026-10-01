using Lens.entity;

namespace BurningKnight.entity.events {
	public class RevivedEvent : Event {
		public Entity Who = null!;
		public Entity WhoDamaged = null!;
	}
}