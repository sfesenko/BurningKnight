using Lens.entity;

namespace BurningKnight.entity.events {
	public class MobTargetChange : Event {
		public Entity Mob = null!;
		public Entity Old = null!;
		public Entity? New;
	}
}