using BurningKnight.entity.component;
using Lens.entity;
using Lens.physics;

namespace BurningKnight.entity.events {
	public class CollisionEndedEvent : Event {
		public Entity Entity = null!;
		public BodyComponent Body = null!;
		public IFixture Fixture = null!;
	}
}
