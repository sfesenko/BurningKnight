using Microsoft.Xna.Framework;

namespace Lens.physics;

/// <summary>Creates the world the game talks to — the one place a physics backend is named.</summary>
public static class PhysicsFactory {
	public static IPhysicsWorld CreateWorld(Vector2 gravity) {
		return new Box2DPhysicsWorld(gravity);
	}
}
