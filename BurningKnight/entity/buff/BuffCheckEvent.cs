using Lens.entity;

namespace BurningKnight.entity.buff {
	public class BuffCheckEvent : Event {
		public Entity Entity = null!;
		public Buff Buff = null!;
	}
}