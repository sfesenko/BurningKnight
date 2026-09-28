using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Lens.physics;

public interface IPhysicsBody {
	Vector2 Position { get; set; }
	float Rotation { get; set; }
	Vector2 LinearVelocity { get; set; }
	float AngularVelocity { get; set; }
	float LinearDamping { get; set; }
	bool FixedRotation { get; set; }
	bool IsBullet { get; set; }
	bool SleepingAllowed { get; set; }
	float Mass { get; set; }
	object UserData { get; set; }

	// Applied to every fixture at once, like the engine's own convenience setters.
	float Friction { set; }
	float Restitution { set; }
	bool IsSensor { set; }

	IReadOnlyList<IFixture> FixtureList { get; }

	void SetTransform(Vector2 position, float rotation);
	IFixture CreatePolygonFixture(Vertices vertices, float density);
	IFixture CreateCircleFixture(float radius, float density, Vector2 offset);
	void DestroyFixture(IFixture fixture);
}
