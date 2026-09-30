#nullable enable

using System;
using Microsoft.Xna.Framework;

namespace Lens.physics;

public interface IPhysicsWorld {
	event Action<IContact>? PreSolve;
	event Action<IContact>? BeginContact;
	event Action<IContact>? EndContact;

	IPhysicsBody CreateBody(Vector2 position, float rotation, BodyType type);
	void RemoveBody(IPhysicsBody body);
	void Step(float dt);
	void Clear();
	void RayCast(Func<IFixture, Vector2, Vector2, float, float> callback, Vector2 point1, Vector2 point2);
	void RenderDebug();
}
