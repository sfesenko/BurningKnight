using System;
using BurningKnight.physics;
using Lens.physics;
using Lens.util;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.component {
	public class RectBodyComponent : BodyComponent {
		public RectBodyComponent(float x, float y, float w, float h, BodyType type = BodyType.Dynamic, bool sensor = false, bool center = false) {
			// Box2D rejects degenerate quads: clamp to a sliver, never hand native code a
			// zero-area polygon.
			w = Math.Max(w, 0.5f);
			h = Math.Max(h, 0.5f);

			if (center) {
				x -= w / 2;
				y -= h / 2;
				Offset = new Vector2(w / 2, h / 2);
			}

			Body = Physics.World!.CreateBody(Vector2.Zero, 0, type);

			Body.FixedRotation = true;
			Body.UserData = this;
			Body.LinearDamping = 0;

			if (type == BodyType.Static) {
				KnockbackModifier = 0;
			}
			
			Body.CreatePolygonFixture(new Vertices(4) {
				new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h)
			}, 1f).IsSensor = sensor;
		}

		public override void Resize(float x, float y, float w, float h, bool center = false) {
			// Clamp instead of skipping: an early return would leave the fixture at the old
			// size while Width/Height moved on; a degenerate quad fails Box2D validation.
			w = Math.Max(w, 0.5f);
			h = Math.Max(h, 0.5f);

			var fixture = Body!.FixtureList[0];
			var sensor = fixture.IsSensor;
			
			Body.DestroyFixture(fixture);

			if (center) {
				x -= w / 2;
				y -= h / 2;
				Offset = new Vector2(w / 2, h / 2);
			}
			
			Body.CreatePolygonFixture(new Vertices(4) {
				new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h)
			}, 1f).IsSensor = sensor;
		}
	}
}