using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using VelcroPhysics.Collision.Shapes;
using VelcroPhysics.Dynamics;

namespace Lens.physics;

public class VelcroPhysicsBody : IPhysicsBody {
	private readonly VelcroPhysicsWorld owner;

	internal readonly Body Body;

	public VelcroPhysicsBody(VelcroPhysicsWorld owner, Body body) {
		this.owner = owner;
		Body = body;
	}

	public Vector2 Position {
		get => Body.Position;
		set => Body.Position = value;
	}

	public float Rotation {
		get => Body.Rotation;
		set => Body.Rotation = value;
	}

	public Vector2 LinearVelocity {
		get => Body.LinearVelocity;
		set => Body.LinearVelocity = value;
	}

	public float AngularVelocity {
		get => Body.AngularVelocity;
		set => Body.AngularVelocity = value;
	}

	public float LinearDamping {
		get => Body.LinearDamping;
		set => Body.LinearDamping = value;
	}

	public bool FixedRotation {
		get => Body.FixedRotation;
		set => Body.FixedRotation = value;
	}

	public bool IsBullet {
		get => Body.IsBullet;
		set => Body.IsBullet = value;
	}

	public bool SleepingAllowed {
		get => Body.SleepingAllowed;
		set => Body.SleepingAllowed = value;
	}

	public float Mass {
		get => Body.Mass;
		set => Body.Mass = value;
	}

	public object UserData {
		get => Body.UserData;
		set => Body.UserData = value;
	}

	public float Friction {
		set => Body.Friction = value;
	}

	public float Restitution {
		set => Body.Restitution = value;
	}

	public bool IsSensor {
		set => Body.IsSensor = value;
	}

	public IReadOnlyList<IFixture> FixtureList {
		get {
			var list = new List<IFixture>(Body.FixtureList.Count);

			foreach (var fixture in Body.FixtureList) {
				list.Add(owner.GetFixture(fixture));
			}

			return list;
		}
	}

	public void SetTransform(Vector2 position, float rotation) {
		Body.SetTransform(position, rotation);
	}

	public IFixture CreatePolygonFixture(Vertices vertices, float density) {
		if (vertices.Count <= 1) {
			throw new ArgumentOutOfRangeException(nameof(vertices), "Too few points to be a polygon");
		}

		var polygon = new PolygonShape(new VelcroPhysics.Shared.Vertices(vertices), density);

		return owner.GetFixture(Body.CreateFixture(polygon));
	}

	public IFixture CreateCircleFixture(float radius, float density, Vector2 offset) {
		var circle = new CircleShape(radius, density) {
			Position = offset
		};

		return owner.GetFixture(Body.CreateFixture(circle));
	}

	public void DestroyFixture(IFixture fixture) {
		Body.DestroyFixture(VelcroPhysicsWorld.Unwrap(fixture));
	}
}
