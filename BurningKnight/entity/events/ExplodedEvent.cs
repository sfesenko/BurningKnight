using Lens.entity;

namespace BurningKnight.entity.events {
	public class ExplodedEvent : Event {
		public Entity Who = null!;
		public Entity Origin = null!;
		public float Damage;
	}
}