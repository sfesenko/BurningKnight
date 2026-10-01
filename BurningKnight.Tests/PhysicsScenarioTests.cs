using System;
using System.Collections.Generic;
using Lens.physics;
using Microsoft.Xna.Framework;
using Xunit;

namespace BurningKnight.Tests;

// Physics behaviour through the IPhysicsWorld seam: build a world, add bodies, step a fixed
// number of times, look at the outcome. Nothing here names a backend, so the same scenarios
// run against the engine in the tree and against whatever replaces it. No assets and no
// graphics are involved — a world is bodies and a clock.
public class PhysicsScenarioTests {
	private const float Dt = 1f / 60f;

	private static IPhysicsWorld NewWorld(Vector2 gravity) {
		return PhysicsFactory.CreateWorld(gravity);
	}

	// Rotation is not what these scenarios are about; fixing it keeps them deterministic.
	private static IPhysicsBody AddBody(IPhysicsWorld world, Vector2 position, BodyType type) {
		var body = world.CreateBody(position, 0, type);

		body.FixedRotation = true;
		body.LinearDamping = 0;

		return body;
	}

	private static IPhysicsBody AddBox(IPhysicsWorld world, Vector2 position, float halfWidth, float halfHeight, BodyType type = BodyType.Dynamic) {
		var body = AddBody(world, position, type);

		body.CreatePolygonFixture(new Vertices {
			new Vector2(-halfWidth, -halfHeight),
			new Vector2(halfWidth, -halfHeight),
			new Vector2(halfWidth, halfHeight),
			new Vector2(-halfWidth, halfHeight)
		}, 1f);

		return body;
	}

	private static IPhysicsBody AddCircle(IPhysicsWorld world, Vector2 position, float radius, BodyType type = BodyType.Dynamic) {
		var body = AddBody(world, position, type);

		body.CreateCircleFixture(radius, 1f, Vector2.Zero);

		return body;
	}

	private static void Step(IPhysicsWorld world, float seconds) {
		var steps = (int) Math.Round(seconds / Dt);

		for (var i = 0; i < steps; i++) {
			world.Step(Dt);
		}
	}

	private static bool Touches(IContact contact, IPhysicsBody body) {
		return ReferenceEquals(contact.FixtureA.Body, body) || ReferenceEquals(contact.FixtureB.Body, body);
	}

	[Fact]
	public void FreeBodyKeepsItsVelocity() {
		var world = NewWorld(Vector2.Zero);
		var body = AddCircle(world, Vector2.Zero, 2);

		body.LinearVelocity = new Vector2(120, 0);
		Step(world, 1);

		Assert.InRange(body.Position.X, 115, 125);
		Assert.InRange(body.Position.Y, -0.5f, 0.5f);
	}

	// The engine in the tree caps a body at 2 units of movement per step (Settings.MaxTranslation,
	// applied to the velocity in the solver). The game's speeds are built around that cap — its
	// projectiles are launched at 10x their nominal speed and then clamped — so a replacement has
	// to cap at the same rate: Box2D's maximumLinearSpeed = 2 * 60 at a 60 Hz step.
	[Fact]
	public void FastBodyMovementIsCappedPerStep() {
		var world = NewWorld(Vector2.Zero);
		var body = AddCircle(world, Vector2.Zero, 2);

		body.LinearVelocity = new Vector2(3000, 0);
		Step(world, 0.2f);

		Assert.InRange(body.Position.X, 23, 25);
	}

	[Fact]
	public void BodyRestsOnStaticFloor() {
		var world = NewWorld(new Vector2(0, 300));
		var floor = AddBox(world, new Vector2(0, 24), 40, 2, BodyType.Static);
		var body = AddBox(world, Vector2.Zero, 2, 2);

		Step(world, 3);

		// The floor's top is y = 22; a four-unit box rests with its centre near y = 20.
		Assert.InRange(body.Position.Y, 18, 21.5f);
		Assert.InRange(Math.Abs(body.LinearVelocity.Y), 0, 10);
		Assert.True(body.Position.Y < floor.Position.Y, $"the body sank into the floor (y = {body.Position.Y})");
	}

	[Fact]
	public void ImpactTransfersMomentum() {
		var world = NewWorld(Vector2.Zero);
		var pusher = AddCircle(world, Vector2.Zero, 4);
		var target = AddCircle(world, new Vector2(30, 0), 4);

		pusher.LinearVelocity = new Vector2(100, 0);
		Step(world, 2);

		Assert.True(target.Position.X > 35, $"the target was not pushed (x = {target.Position.X})");
		Assert.True(pusher.Position.X < target.Position.X, "the pusher ended up in front of the target");
	}

	[Fact]
	public void BulletStopsAtTheWall() {
		var world = NewWorld(Vector2.Zero);
		AddBox(world, new Vector2(100, 0), 1, 20, BodyType.Static);

		var bullet = AddCircle(world, Vector2.Zero, 1);

		bullet.IsBullet = true;
		bullet.LinearVelocity = new Vector2(6000, 0);
		Step(world, 2);

		Assert.InRange(bullet.Position.X, 90, 100);
	}

	[Fact]
	public void SensorFiresBeginAndEnd() {
		var world = NewWorld(Vector2.Zero);
		var sensor = AddBox(world, Vector2.Zero, 10, 10, BodyType.Static);
		var walker = AddCircle(world, new Vector2(-40, 0), 2);

		sensor.IsSensor = true;
		walker.LinearVelocity = new Vector2(60, 0);

		var begins = 0;
		var ends = 0;

		world.BeginContact += c => {
			if (Touches(c, sensor)) {
				begins++;
			}
		};

		world.EndContact += c => {
			if (Touches(c, sensor)) {
				ends++;
			}
		};

		Step(world, 2);

		Assert.True(begins >= 1, "no begin contact for the sensor");
		Assert.True(ends >= 1, "no end contact for the sensor");
		Assert.True(walker.Position.X > 50, $"the sensor blocked the body (x = {walker.Position.X})");
	}

	[Fact]
	public void UserDataSurvivesContacts() {
		var world = NewWorld(Vector2.Zero);
		var pusher = AddCircle(world, Vector2.Zero, 4);
		var target = AddCircle(world, new Vector2(30, 0), 4);
		var marker = new object();

		target.UserData = marker;
		pusher.LinearVelocity = new Vector2(100, 0);

		object? seen = null;

		world.BeginContact += c => {
			seen ??= c.FixtureA.Body.UserData ?? c.FixtureB.Body.UserData;
		};

		Step(world, 1);

		Assert.Same(marker, seen);
	}

	[Fact]
	public void RayCastFindsTheNearestFixture() {
		var world = NewWorld(Vector2.Zero);
		var near = AddBox(world, new Vector2(100, 0), 5, 5, BodyType.Static);

		AddBox(world, new Vector2(150, 0), 5, 5, BodyType.Static);
		var hits = new List<(IPhysicsBody Body, Vector2 Point, float Fraction)>();
		var min = 1f;

		// The game's own ray-cast shape: remember the best fraction and return it to clip the ray.
		world.RayCast((fixture, point, normal, fraction) => {
			hits.Add((fixture.Body, point, fraction));

			if (fraction < min) {
				min = fraction;
			}

			return min;
		}, Vector2.Zero, new Vector2(200, 0));

		// The backend may report candidates past the clip, so the nearest hit is the one with the
		// smallest fraction — which is exactly what the game's callbacks keep.
		Assert.Contains(hits, h => ReferenceEquals(h.Body, near) && Math.Abs(h.Fraction - 0.475f) < 0.05f);

		var nearest = hits[0];

		foreach (var h in hits) {
			if (h.Fraction < nearest.Fraction) {
				nearest = h;
			}
		}

		Assert.Same(near, nearest.Body);
		Assert.InRange(nearest.Point.X, 93, 97);
	}

	[Fact]
	public void PreSolveCanDisableAContact() {
		var world = NewWorld(Vector2.Zero);
		var wall = AddBox(world, new Vector2(40, 0), 2, 20, BodyType.Static);
		var body = AddCircle(world, Vector2.Zero, 2);

		body.LinearVelocity = new Vector2(60, 0);

		world.PreSolve += c => {
			if (Touches(c, wall)) {
				c.Enabled = false;
			}
		};

		Step(world, 1.5f);

		Assert.True(body.Position.X > 60, $"the disabled contact still blocked the body (x = {body.Position.X})");
	}

	[Fact]
	public void HeavyBodyResistsPush() {
		var world = NewWorld(Vector2.Zero);
		var heavy = AddCircle(world, new Vector2(30, 0), 4);
		var light = AddCircle(world, Vector2.Zero, 4);

		heavy.Mass = 1000000f;
		light.LinearVelocity = new Vector2(120, 0);
		Step(world, 1);

		Assert.InRange(heavy.Position.X, 29.5f, 31);
		Assert.True(light.Position.X < heavy.Position.X, "the light body ended up in front of the heavy one");
	}

	[Fact]
	public void FixtureTeardownAndWorldClearAreSafe() {
		var world = NewWorld(Vector2.Zero);
		var body = AddBox(world, Vector2.Zero, 2, 2);
		var extra = body.CreateCircleFixture(2, 1, Vector2.Zero);

		// The teardown path the hand-patches in PATCHES.md exist for: a destroyed fixture,
		// a removed body, then a clear.
		body.DestroyFixture(extra);
		world.RemoveBody(body);
		world.Step(Dt);

		AddCircle(world, new Vector2(10, 0), 2);
		world.Step(Dt);
		world.Clear();

		var survivor = AddCircle(world, Vector2.Zero, 2);

		survivor.LinearVelocity = new Vector2(60, 0);
		world.Step(Dt);

		Assert.True(survivor.Position.X > 0, "the world did not run after Clear");
	}

	[Fact]
	public void PositionCanBeSetOneAxisAtATime() {
		var world = NewWorld(Vector2.Zero);
		var body = AddBox(world, new Vector2(10, 10), 2, 2);

		body.Position = new Vector2(20, body.Position.Y);
		body.Position = new Vector2(body.Position.X, 30);
		body.SetTransform(new Vector2(40, 40), 0);

		Assert.InRange(body.Position.X, 39.9f, 40.1f);
		Assert.InRange(body.Position.Y, 39.9f, 40.1f);
	}
}
