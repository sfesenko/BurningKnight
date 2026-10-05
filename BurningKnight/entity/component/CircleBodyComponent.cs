using System;
using BurningKnight.physics;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.component {
	public class CircleBodyComponent : BodyComponent {
		public CircleBodyComponent(float x, float y, float r, BodyType type = BodyType.Dynamic, bool sensor = false, bool center = false) {
			// Box2D rejects degenerate circles: clamp to a sliver.
			r = Math.Max(r, 0.25f);

			if (center) {
				x -= r;
				y -= r;
				Offset = new Vector2(r, r);
			}

			Body = Physics.World!.CreateBody(Vector2.Zero, 0, type);
			Body.FixedRotation = true;
			Body.UserData = this;
			Body.LinearDamping = 0;

			Body.CreateCircleFixture(r, 1, new Vector2(x + r, y + r)).IsSensor = sensor;
		}

		public override void Resize(float x, float y, float w, float h, bool center = false) {
			// Clamp instead of skipping: the fixture must match the new size, and Box2D
			// rejects degenerate circles.
			var r = Math.Max(w / 2f, 0.25f);

			var fixture = Body!.FixtureList[0];
			var sensor = fixture.IsSensor;
			
			Body.DestroyFixture(fixture);

			if (center) {
				x -= r;
				y -= r;
				Offset = new Vector2(r, r);
			}
			
			Body.CreateCircleFixture(r, 1, new Vector2(x + r, y + r)).IsSensor = sensor;
		}
	}
}