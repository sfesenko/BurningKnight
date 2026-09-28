using VelcroPhysics.Collision.ContactSystem;

namespace Lens.physics;

// One instance per world, reused for every event: contacts fire per pair per step, so wrapping
// them per event would allocate in the hot path. The wrappers are only valid during the callback.
public class VelcroContact : IContact {
	private readonly VelcroFixture fixtureA;
	private readonly VelcroFixture fixtureB;

	private Contact contact = null!;

	public VelcroContact(VelcroPhysicsWorld owner) {
		fixtureA = new VelcroFixture(owner, null!);
		fixtureB = new VelcroFixture(owner, null!);
	}

	public IFixture FixtureA => fixtureA;
	public IFixture FixtureB => fixtureB;

	public bool Enabled {
		get => contact.Enabled;
		set => contact.Enabled = value;
	}

	internal void Set(Contact contact) {
		this.contact = contact;

		fixtureA.Fixture = contact.FixtureA;
		fixtureB.Fixture = contact.FixtureB;
	}
}
