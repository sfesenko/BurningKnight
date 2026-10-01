using Box2D.NET;
using Microsoft.Xna.Framework;
using static Box2D.NET.B2Shapes;

namespace Lens.physics;

public class Box2DPhysicsFixture : IFixture {
	private readonly Box2DPhysicsBody body;
	private readonly bool isCircle;

	internal B2ShapeId ShapeId;

	public Box2DPhysicsFixture(Box2DPhysicsBody body, B2ShapeId shapeId, bool isCircle) {
		this.body = body;
		this.isCircle = isCircle;
		ShapeId = shapeId;
	}

	public IPhysicsBody Body => body;

	public bool IsSensor {
		get => b2Shape_IsSensor(ShapeId);
		set {
			if (IsSensor != value) {
				Recreate(value);
			}
		}
	}

	public AABB GetAABB() {
		var aabb = b2Shape_GetAABB(ShapeId);

		return new AABB {
			LowerBound = new Vector2(aabb.lowerBound.X, aabb.lowerBound.Y),
			UpperBound = new Vector2(aabb.upperBound.X, aabb.upperBound.Y)
		};
	}

	// Box2D 3 fixes the sensor flag when a shape is created, so flipping it means recreating the
	// shape from its current geometry and material. The game toggles sensors on doors, platforms
	// and statues, so this is the only way to keep the seam's mutable IsSensor honest.
	private void Recreate(bool sensor) {
		var def = Box2DPhysicsWorld.DefaultShapeDef(b2Shape_GetDensity(ShapeId));

		def.isSensor = sensor;
		def.material.friction = b2Shape_GetFriction(ShapeId);
		def.material.restitution = b2Shape_GetRestitution(ShapeId);

		var replacement = isCircle
			? b2CreateCircleShape(body.BodyId, def, b2Shape_GetCircle(ShapeId))
			: b2CreatePolygonShape(body.BodyId, def, b2Shape_GetPolygon(ShapeId));

		b2DestroyShape(ShapeId, true);
		ShapeId = replacement;
	}
}
