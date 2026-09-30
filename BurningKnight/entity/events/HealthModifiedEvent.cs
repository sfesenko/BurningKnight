using Lens.entity;

namespace BurningKnight.entity.events {
	public class HealthModifiedEvent : Event {
		public float Amount;
		public Entity From = null!;
		public Entity Who = null!;
		public bool Default = true;
		public DamageType Type = DamageType.Regular;
		public HealthType HealthType;
		public bool PressedForBomb;
	}
}