using BurningKnight.entity.component;
using Lens.entity;
using Lens.physics;

namespace BurningKnight.entity.events {
	public class CollisionStartedEvent : Event {
		public Entity Entity;
		public BodyComponent Body;
		public IFixture Fixture;
	}
}
