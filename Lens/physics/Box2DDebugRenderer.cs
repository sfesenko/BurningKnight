using Box2D.NET;
using Lens.graphics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Lens.physics;

public class Box2DDebugRenderer {
	private const float Alpha = 0.7f;

	public Color DefaultShapeColor = new Color(0.9f, 0.7f, 0.7f, Alpha);
	public Color InactiveShapeColor = new Color(0.5f, 0.5f, 0.3f, Alpha);
	public Color KinematicShapeColor = new Color(0.5f, 0.5f, 0.9f, Alpha);
	public Color SleepingShapeColor = new Color(0.6f, 0.6f, 0.6f, Alpha);
	public Color StaticShapeColor = new Color(0.5f, 0.9f, 0.5f, Alpha);

	private readonly Box2DPhysicsWorld owner;

	public Box2DDebugRenderer(Box2DPhysicsWorld owner) {
		this.owner = owner;
	}

	public void DrawDebugData() {
		foreach (var body in owner.Bodies) {
			var color = ColorFor(body);

			foreach (var fixture in body.Fixtures) {
				DrawFixture(body, fixture, color);
			}
		}
	}

	private Color ColorFor(Box2DPhysicsBody body) {
		if (!b2Body_IsEnabled(body.BodyId)) {
			return InactiveShapeColor;
		}

		switch (b2Body_GetType(body.BodyId)) {
			case B2BodyType.b2_staticBody:
				return StaticShapeColor;

			case B2BodyType.b2_kinematicBody:
				return KinematicShapeColor;

			default:
				return b2Body_IsAwake(body.BodyId) ? DefaultShapeColor : SleepingShapeColor;
		}
	}

	private static void DrawFixture(Box2DPhysicsBody body, Box2DPhysicsFixture fixture, Color color) {
		if (b2Shape_GetType(fixture.ShapeId) == B2ShapeType.b2_circleShape) {
			var circle = b2Shape_GetCircle(fixture.ShapeId);
			var center = b2Body_GetWorldPoint(body.BodyId, circle.center);

			Graphics.Batch.DrawCircle(new Vector2(center.X, center.Y), circle.radius, 32, color);

			return;
		}

		var polygon = b2Shape_GetPolygon(fixture.ShapeId);
		var vertices = new Vector2[polygon.count];

		for (var i = 0; i < polygon.count; i++) {
			var point = b2Body_GetWorldPoint(body.BodyId, polygon.vertices[i]);

			vertices[i] = new Vector2(point.X, point.Y);
		}

		Graphics.Batch.DrawPolygon(Vector2.Zero, vertices, color);
	}
}
