using System;
using System.Collections.Generic;
using Box2D.NET;
using Lens.util;
using Microsoft.Xna.Framework;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Lens.physics;

// The Box2D.NET backend behind IPhysicsWorld. Box2D 3 talks in ids and free functions, so this
// adapter keeps the game's object-shaped view: bodies and fixtures are wrappers the game holds,
// and the events Box2D buffers after a step are dispatched through the seam's callbacks.
public class Box2DPhysicsWorld : IPhysicsWorld {
	// The engine in the tree caps a body at 2 units of movement per step (Settings.MaxTranslation,
	// applied to the velocity in the solver) and the game's speeds are tuned around that cap.
	// Box2D's cap is in units per second, so it gets the same rate at the 60 Hz step the game runs.
	private const float MaxLinearSpeed = 2f * 60f;

	private readonly Vector2 gravity;
	private readonly Dictionary<B2BodyId, Box2DPhysicsBody> bodies = new();
	private readonly List<IPhysicsBody> toRemove = new();
	private readonly Box2DPhysicsContact contact;
	private readonly Box2DDebugRenderer debug;

	private B2WorldId world;
	private bool locked;

	public event Action<IContact>? PreSolve;
	public event Action<IContact>? BeginContact;
	public event Action<IContact>? EndContact;

	public Box2DPhysicsWorld(Vector2 gravity) {
		this.gravity = gravity;

		contact = new Box2DPhysicsContact(this);
		debug = new Box2DDebugRenderer(this);
		world = CreateWorld();
	}

	public IPhysicsBody CreateBody(Vector2 position, float rotation, BodyType type) {
		var def = b2DefaultBodyDef();

		def.type = Convert(type);
		def.position = new B2Vec2(position.X, position.Y);
		def.rotation = b2MakeRot(rotation);

		var body = new Box2DPhysicsBody(b2CreateBody(world, def));

		bodies[body.BodyId] = body;

		return body;
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
			b2World_Step(world, dt, 4);
		} finally {
			locked = false;
		}

		DispatchEvents();
	}

	public void Clear() {
		if (locked) {
			Log.Error("World was locked when destroying");
		}

		RemoveBodies();

		b2DestroyWorld(world);
		bodies.Clear();

		world = CreateWorld();
	}

	public void RayCast(Func<IFixture, Vector2, Vector2, float, float> callback, Vector2 point1, Vector2 point2) {
		b2World_CastRay(world, new B2Vec2(point1.X, point1.Y), new B2Vec2(point2.X - point1.X, point2.Y - point1.Y),
			b2DefaultQueryFilter(), RayCastCallback, new RayCastContext(this, callback));
	}

	public void RenderDebug() {
		debug.DrawDebugData();
	}

	internal IEnumerable<Box2DPhysicsBody> Bodies => bodies.Values;

	internal Box2DPhysicsBody GetBody(B2BodyId id) {
		if (bodies.TryGetValue(id, out var body)) {
			return body;
		}

		// A view over a body this world did not create; the game only reads from it.
		body = new Box2DPhysicsBody(id);
		bodies[id] = body;

		return body;
	}

	internal Box2DPhysicsFixture GetFixture(B2ShapeId id) {
		return GetBody(b2Shape_GetBody(id)).FindFixture(id);
	}

	internal static B2ShapeDef DefaultShapeDef(float density) {
		var def = b2DefaultShapeDef();

		def.density = density;
		def.material.friction = 0.2f;
		def.enableContactEvents = true;
		def.enablePreSolveEvents = true;
		// Box2D only reports a sensor touch when both shapes of the pair opt in, so every shape
		// carries the flag; it is only consulted for sensor pairs.
		def.enableSensorEvents = true;

		return def;
	}

	private B2WorldId CreateWorld() {
		var def = b2DefaultWorldDef();

		def.gravity = new B2Vec2(gravity.X, gravity.Y);
		def.maximumLinearSpeed = MaxLinearSpeed;

		var id = b2CreateWorld(def);

		b2World_SetPreSolveCallback(id, OnPreSolve, this);

		return id;
	}

	private void RemoveBodies() {
		if (locked || toRemove.Count == 0) {
			return;
		}

		foreach (var wrapped in toRemove) {
			var body = (Box2DPhysicsBody) wrapped;

			if (b2Body_IsValid(body.BodyId)) {
				b2DestroyBody(body.BodyId);
			}

			bodies.Remove(body.BodyId);
		}

		toRemove.Clear();
	}

	private void DispatchEvents() {
		var contacts = b2World_GetContactEvents(world);

		for (var i = 0; i < contacts.beginCount; i++) {
			var e = contacts.beginEvents[i];

			contact.Set(e.shapeIdA, e.shapeIdB);
			BeginContact?.Invoke(contact);
		}

		for (var i = 0; i < contacts.endCount; i++) {
			var e = contacts.endEvents[i];

			contact.Set(e.shapeIdA, e.shapeIdB);
			EndContact?.Invoke(contact);
		}

		// Sensors never take part in contact events, so their begin/end touches are the only
		// place the game hears about them.
		var sensors = b2World_GetSensorEvents(world);

		for (var i = 0; i < sensors.beginCount; i++) {
			var e = sensors.beginEvents[i];

			contact.Set(e.sensorShapeId, e.visitorShapeId);
			BeginContact?.Invoke(contact);
		}

		for (var i = 0; i < sensors.endCount; i++) {
			var e = sensors.endEvents[i];

			contact.Set(e.sensorShapeId, e.visitorShapeId);
			EndContact?.Invoke(contact);
		}
	}

	private bool OnPreSolve(B2ShapeId a, B2ShapeId b, B2Vec2 point, B2Vec2 normal, object context) {
		contact.Set(a, b);
		PreSolve?.Invoke(contact);

		return contact.Enabled;
	}

	private static float RayCastCallback(B2ShapeId shapeId, B2Vec2 point, B2Vec2 normal, float fraction, object context) {
		var ray = (RayCastContext) context;

		return ray.Callback(ray.World.GetFixture(shapeId), new Vector2(point.X, point.Y),
			new Vector2(normal.X, normal.Y), fraction);
	}

	private static B2BodyType Convert(BodyType type) {
		switch (type) {
			case BodyType.Static:
				return B2BodyType.b2_staticBody;

			case BodyType.Kinematic:
				return B2BodyType.b2_kinematicBody;

			default:
				return B2BodyType.b2_dynamicBody;
		}
	}

	private class RayCastContext {
		public readonly Box2DPhysicsWorld World;
		public readonly Func<IFixture, Vector2, Vector2, float, float> Callback;

		public RayCastContext(Box2DPhysicsWorld world, Func<IFixture, Vector2, Vector2, float, float> callback) {
			World = world;
			Callback = callback;
		}
	}
}
