namespace Lens.physics;

public interface IFixture {
	IPhysicsBody Body { get; }
	bool IsSensor { get; set; }
	AABB GetAABB();
}
