using Lens.entity;

namespace BurningKnight.entity.events {
	public class DiedEvent : Event {
		public Entity From = null!;
		public Entity Who = null!;
		public bool BlockClear;
		public DamageType DamageType;
	}
}