using System;
using System.Collections.Generic;
using Box2D.NET;
using Microsoft.Xna.Framework;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;

namespace Lens.physics;

public class Box2DPhysicsBody : IPhysicsBody {
	private readonly List<Box2DPhysicsFixture> fixtures = new();
	private object userData = null!;

	internal readonly B2BodyId BodyId;

	public Box2DPhysicsBody(B2BodyId bodyId) {
		BodyId = bodyId;
	}

	public Vector2 Position {
		get {
			var p = b2Body_GetPosition(BodyId);

			return new Vector2(p.X, p.Y);
		}
		set => b2Body_SetTransform(BodyId, new B2Vec2(value.X, value.Y), b2Body_GetRotation(BodyId));
	}

	public float Rotation {
		get => b2Rot_GetAngle(b2Body_GetRotation(BodyId));
		set => b2Body_SetTransform(BodyId, b2Body_GetPosition(BodyId), b2MakeRot(value));
	}

	public Vector2 LinearVelocity {
		get {
			var v = b2Body_GetLinearVelocity(BodyId);

			return new Vector2(v.X, v.Y);
		}
		set => b2Body_SetLinearVelocity(BodyId, new B2Vec2(value.X, value.Y));
	}

	public float AngularVelocity {
		get => b2Body_GetAngularVelocity(BodyId);
		set => b2Body_SetAngularVelocity(BodyId, value);
	}

	public float LinearDamping {
		get => b2Body_GetLinearDamping(BodyId);
		set => b2Body_SetLinearDamping(BodyId, value);
	}

	public bool FixedRotation {
		get => b2Body_GetMotionLocks(BodyId).angularZ;
		set {
			var locks = b2Body_GetMotionLocks(BodyId);

			locks.angularZ = value;
			b2Body_SetMotionLocks(BodyId, locks);

			if (value) {
				b2Body_SetAngularVelocity(BodyId, 0);
			}
		}
	}

	public bool IsBullet {
		get => b2Body_IsBullet(BodyId);
		set => b2Body_SetBullet(BodyId, value);
	}

	public bool SleepingAllowed {
		get => b2Body_IsSleepEnabled(BodyId);
		set => b2Body_EnableSleep(BodyId, value);
	}

	public float Mass {
		get => b2Body_GetMass(BodyId);
		set {
			var data = b2Body_GetMassData(BodyId);

			data.mass = value;
			b2Body_SetMassData(BodyId, data);
		}
	}

	public object UserData {
		get => userData;
		set => userData = value;
	}

	public float Friction {
		set {
			foreach (var fixture in fixtures) {
				b2Shape_SetFriction(fixture.ShapeId, value);
			}
		}
	}

	public float Restitution {
		set {
			foreach (var fixture in fixtures) {
				b2Shape_SetRestitution(fixture.ShapeId, value);
			}
		}
	}

	public bool IsSensor {
		set {
			foreach (var fixture in fixtures) {
				fixture.IsSensor = value;
			}
		}
	}

	public IReadOnlyList<IFixture> FixtureList => fixtures;

	internal List<Box2DPhysicsFixture> Fixtures => fixtures;

	public void SetTransform(Vector2 position, float rotation) {
		b2Body_SetTransform(BodyId, new B2Vec2(position.X, position.Y), b2MakeRot(rotation));
	}

	public IFixture CreatePolygonFixture(Vertices vertices, float density) {
		if (vertices.Count <= 1) {
			throw new ArgumentOutOfRangeException(nameof(vertices), "Too few points to be a polygon");
		}

		var points = new B2Vec2[vertices.Count];

		for (var i = 0; i < vertices.Count; i++) {
			points[i] = new B2Vec2(vertices[i].X, vertices[i].Y);
		}

		var hull = b2ComputeHull(points, points.Length);

		// The game builds polygons from tile outlines and catches the failure to skip them, so
		// a hull that cannot be built has to throw, not create an empty fixture.
		if (hull.count == 0) {
			throw new ArgumentException("The vertices do not form a convex polygon", nameof(vertices));
		}

		return AddFixture(new Box2DPhysicsFixture(this,
			b2CreatePolygonShape(BodyId, Box2DPhysicsWorld.DefaultShapeDef(density), b2MakePolygon(hull, 0f)), false));
	}

	public IFixture CreateCircleFixture(float radius, float density, Vector2 offset) {
		var circle = new B2Circle(new B2Vec2(offset.X, offset.Y), radius);

		return AddFixture(new Box2DPhysicsFixture(this,
			b2CreateCircleShape(BodyId, Box2DPhysicsWorld.DefaultShapeDef(density), circle), true));
	}

	public void DestroyFixture(IFixture fixture) {
		var wrapped = (Box2DPhysicsFixture) fixture;

		b2DestroyShape(wrapped.ShapeId, true);
		fixtures.Remove(wrapped);
	}

	internal Box2DPhysicsFixture FindFixture(B2ShapeId id) {
		foreach (var fixture in fixtures) {
			if (fixture.ShapeId.index1 == id.index1 && fixture.ShapeId.world0 == id.world0 &&
			    fixture.ShapeId.generation == id.generation) {
				return fixture;
			}
		}

		// Every shape is created through this body; a miss is a stale id from a callback, kept
		// readable rather than thrown on.
		return new Box2DPhysicsFixture(this, id, false);
	}

	private Box2DPhysicsFixture AddFixture(Box2DPhysicsFixture fixture) {
		fixtures.Add(fixture);

		return fixture;
	}
}
