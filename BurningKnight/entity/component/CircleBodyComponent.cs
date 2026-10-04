using System;
using BurningKnight.physics;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.component {
	public class CircleBodyComponent : BodyComponent {
		public CircleBodyComponent(float x, float y, float r, BodyType type = BodyType.Dynamic, bool sensor = false, bool center = false) {
			// Box2D rejects degenerate circles: clamp to a sliver rather than hand
			// native code a zero radius.
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
			// Clamp, don't skip: a silent return would leave the size fields already
			// shrunk while the fixture keeps its old size. A degenerate circle fails
			// Box2D validation and would leave the body fixture-less.
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