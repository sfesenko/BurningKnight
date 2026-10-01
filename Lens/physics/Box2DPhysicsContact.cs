using Box2D.NET;

namespace Lens.physics;

// One instance per world, reused for every event: contacts fire per pair per step, so wrapping
// them per event would allocate in the hot path. The wrappers are only valid during the callback.
public class Box2DPhysicsContact : IContact {
	private readonly Box2DPhysicsWorld owner;

	private Box2DPhysicsFixture fixtureA = null!;
	private Box2DPhysicsFixture fixtureB = null!;
	private bool enabled = true;

	public Box2DPhysicsContact(Box2DPhysicsWorld owner) {
		this.owner = owner;
	}

	public IFixture FixtureA => fixtureA;
	public IFixture FixtureB => fixtureB;

	public bool Enabled {
		get => enabled;
		set => enabled = value;
	}

	internal void Set(B2ShapeId a, B2ShapeId b) {
		fixtureA = owner.GetFixture(a);
		fixtureB = owner.GetFixture(b);
		enabled = true;
	}
}
