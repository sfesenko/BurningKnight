#nullable enable

using System;
using System.Collections.Generic;
using Lens.util;
using Microsoft.Xna.Framework;
using VelcroPhysics.Collision.ContactSystem;
using VelcroPhysics.Collision.Narrowphase;
using VelcroPhysics.Dynamics;
using VelcroPhysics.Factories;
using VelcroBodyType = VelcroPhysics.Dynamics.BodyType;

namespace Lens.physics;

public class VelcroPhysicsWorld : IPhysicsWorld {
	private readonly World world;
	private readonly PhysicsDebugRenderer debug;
	private readonly List<IPhysicsBody> toRemove = new();
	private readonly Dictionary<Body, VelcroPhysicsBody> bodies = new();
	private readonly VelcroContact contact;

	private bool locked;

	public event Action<IContact>? PreSolve;
	public event Action<IContact>? BeginContact;
	public event Action<IContact>? EndContact;

	public VelcroPhysicsWorld(Vector2 gravity) {
		world = new World(gravity);
		debug = new PhysicsDebugRenderer(world);
		contact = new VelcroContact(this);

		world.ContactManager.PreSolve += OnPreSolve;
		world.ContactManager.BeginContact += OnBeginContact;
		world.ContactManager.EndContact += OnEndContact;
	}

	public IPhysicsBody CreateBody(Vector2 position, float rotation, BodyType type) {
		var body = BodyFactory.CreateBody(world, position, rotation, Convert(type));
		var wrapped = new VelcroPhysicsBody(this, body);

		bodies[body] = wrapped;
		return wrapped;
	}

	public void RemoveBody(IPhysicsBody body) {
		if (!toRemove.Contains(body)) {
			toRemove.Add(body);
		}
	}

	public void Step(float dt) {
		RemoveBodies();

		// Cleared even when a contact callback throws, otherwise the world stays locked and
		// RemoveBodies silently stops draining for the rest of the process.
		locked = true;

		try {
			world.Step(dt);
		} finally {
			locked = false;
		}
	}

	public void Clear() {
		if (locked) {
			Log.Error("World was locked when destroying");
		}

		RemoveBodies();
		world.Clear();
		bodies.Clear();
	}

	public void RayCast(Func<IFixture, Vector2, Vector2, float, float> callback, Vector2 point1, Vector2 point2) {
		world.RayCast((fixture, point, normal, fraction) => callback(GetFixture(fixture), point, normal, fraction), point1, point2);
	}

	public void RenderDebug() {
		debug.DrawDebugData();
	}

	internal VelcroPhysicsBody GetBody(Body body) {
		if (bodies.TryGetValue(body, out var wrapped)) {
			return wrapped;
		}

		wrapped = new VelcroPhysicsBody(this, body);
		bodies[body] = wrapped;

		return wrapped;
	}

	internal VelcroFixture GetFixture(Fixture fixture) {
		return new VelcroFixture(this, fixture);
	}

	internal static Fixture Unwrap(IFixture fixture) {
		return ((VelcroFixture) fixture).Fixture;
	}

	private void RemoveBodies() {
		if (locked || toRemove.Count == 0) {
			return;
		}

		foreach (var wrapped in toRemove) {
			var body = ((VelcroPhysicsBody) wrapped).Body;

			world.RemoveBody(body);
			bodies.Remove(body);
		}

		toRemove.Clear();
	}

	private void OnPreSolve(Contact c, ref Manifold oldManifold) {
		contact.Set(c);
		PreSolve?.Invoke(contact);

		c.Enabled = contact.Enabled;
	}

	private bool OnBeginContact(Contact c) {
		contact.Set(c);
		BeginContact?.Invoke(contact);

		return true;
	}

	private void OnEndContact(Contact c) {
		contact.Set(c);
		EndContact?.Invoke(contact);
	}

	private static VelcroBodyType Convert(BodyType type) {
		switch (type) {
			case BodyType.Static:
				return VelcroBodyType.Static;

			case BodyType.Kinematic:
				return VelcroBodyType.Kinematic;

			default:
				return VelcroBodyType.Dynamic;
		}
	}
}
