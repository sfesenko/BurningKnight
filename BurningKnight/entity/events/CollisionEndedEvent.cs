using BurningKnight.entity.component;
using Lens.entity;

namespace BurningKnight.entity.events {
	public class CollisionEndedEvent : Event {
		public Entity Entity;
		public BodyComponent Body;
	}
}