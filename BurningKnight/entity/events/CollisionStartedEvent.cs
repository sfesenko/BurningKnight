using BurningKnight.entity.component;
using Lens.entity;

namespace BurningKnight.entity.events {
	public class CollisionStartedEvent : Event {
		public Entity Entity;
		public BodyComponent Body;
	}
}