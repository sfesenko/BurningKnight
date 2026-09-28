using VelcroPhysics.Dynamics;

namespace Lens.physics;

public class VelcroFixture : IFixture {
	private readonly VelcroPhysicsWorld owner;

	// Mutable: the contact wrapper reuses two of these instead of allocating per contact event.
	internal Fixture Fixture;

	public VelcroFixture(VelcroPhysicsWorld owner, Fixture fixture) {
		this.owner = owner;
		Fixture = fixture;
	}

	public IPhysicsBody Body => owner.GetBody(Fixture.Body);

	public bool IsSensor {
		get => Fixture.IsSensor;
		set => Fixture.IsSensor = value;
	}

	public AABB GetAABB() {
		Fixture.GetAABB(out var aabb, 0);

		return new AABB {
			LowerBound = aabb.LowerBound,
			UpperBound = aabb.UpperBound
		};
	}
}
